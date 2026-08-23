using System.Collections.Generic;
using UnityEngine;
using PastryWorld.Core;
using PastryWorld.Narrative;

namespace PastryWorld.Reality
{
    /// <summary>
    /// 选择旗标注册表（T20）。静态位掩码累积。
    /// Demo 旗标示例（自定义 int 常量，最多 32 个）：
    /// 0x01 翻看旧照片 / 0x02 接起电话 / 0x04 沉默挂断 …
    /// </summary>
    public static class ChoiceFlagRegistry
    {
        private static readonly Dictionary<int, string> _flagNames = new Dictionary<int, string>();

        /// <summary>当前全部旗标（位掩码）。</summary>
        public static int AllFlags { get; private set; }

        public static bool Has(int flag) => (AllFlags & flag) == flag;

        /// <summary>置位旗标（重复置位无害）。</summary>
        public static bool SetFlag(int flag, string flagName)
        {
            if (Has(flag)) return false;

            AllFlags |= flag;
            if (!string.IsNullOrEmpty(flagName))
                _flagNames[flag] = flagName;

            EventBus.Default.Publish(new ChoiceFlagsChangedEvent
            {
                changedFlag = flag,
                flagName = flagName,
                allFlags = AllFlags
            });
            return true;
        }

        /// <summary>旗标可读名（调试/图鉴用）。</summary>
        public static string GetName(int flag)
        {
            return _flagNames.TryGetValue(flag, out var name) ? name : $"flag_{flag:X}";
        }

        /// <summary>清空（测试用）。</summary>
        public static void Reset()
        {
            AllFlags = 0;
            _flagNames.Clear();
        }
    }

    /// <summary>
    /// 选择规则（T20）：把对话分支选择 / 独白读完 映射为旗标 + 情感权重。
    /// source=DialogueChoice 时按 dialogueId+nodeId+choiceIndex 精确匹配；
    /// source=MonologueEnd 时按 monologueId 匹配。
    /// </summary>
    [System.Serializable]
    public class ChoiceRule
    {
        public enum Source
        {
            DialogueChoice,
            MonologueEnd
        }

        public Source source = Source.DialogueChoice;

        [Tooltip("对话 ID（DialogueChoice）/ 独白 ID（MonologueEnd）")]
        public string contentId;

        [Tooltip("节点 ID（仅 DialogueChoice）")]
        public string nodeId;

        [Tooltip("选项下标，-1 = 匹配任意选项（仅 DialogueChoice）")]
        public int choiceIndex = -1;

        [Tooltip("旗标位（如 0x02）")]
        public int flag;

        public string flagName;

        [Tooltip("情感权重 -1~1：正=温暖，负=冷灰（联动 T18 色调）")]
        [Range(-1f, 1f)] public float emotionalWeight;
    }

    /// <summary>
    /// 选择导演（T20）。订阅 DialogueChoiceMadeEvent / RealityMonologueEndedEvent，
    /// 按规则置位旗标 → 累积情感权重 → 联动 T18 SceneToneManager（色调）
    /// + 场景变体组（VariantGroup 按情感进度三档切换 SetActive）。
    /// </summary>
    public class RealityChoiceDirector : MonoBehaviour
    {
        /// <summary>场景变体组：情感进度落入该档位时激活组内物件。</summary>
        [System.Serializable]
        public class VariantGroup
        {
            public ToneVariant variant;
            public GameObject[] objects;
        }

        [Header("选择规则表")]
        [SerializeField] private List<ChoiceRule> _rules = new List<ChoiceRule>();

        [Header("场景变体组（可选）")]
        [SerializeField] private List<VariantGroup> _variantGroups = new List<VariantGroup>();

        [Header("色调过渡")]
        [SerializeField] private float _toneDuration = 1.5f;

        private IEventBus _eventBus;
        private float _emotionalScore;

        void Awake()
        {
            _eventBus = EventBus.Default;
        }

        void OnEnable()
        {
            _eventBus.Subscribe<DialogueChoiceMadeEvent>(OnDialogueChoice);
            _eventBus.Subscribe<RealityMonologueEndedEvent>(OnMonologueEnded);
            _eventBus.Subscribe<ChoiceFlagsChangedEvent>(OnFlagsChanged);
        }

        void OnDisable()
        {
            _eventBus.Unsubscribe<DialogueChoiceMadeEvent>(OnDialogueChoice);
            _eventBus.Unsubscribe<RealityMonologueEndedEvent>(OnMonologueEnded);
            _eventBus.Unsubscribe<ChoiceFlagsChangedEvent>(OnFlagsChanged);
        }

        /// <summary>当前情感进度 0（冷灰）~1（温暖）。</summary>
        public float EmotionalProgress => Mathf.Clamp01(_emotionalScore);

        private void OnDialogueChoice(DialogueChoiceMadeEvent evt)
        {
            foreach (var rule in _rules)
            {
                if (rule.source != ChoiceRule.Source.DialogueChoice) continue;
                if (rule.contentId != evt.dialogueId) continue;
                if (!string.IsNullOrEmpty(rule.nodeId) && rule.nodeId != evt.nodeId) continue;
                if (rule.choiceIndex >= 0 && rule.choiceIndex != evt.choiceIndex) continue;

                ApplyRule(rule);
                return; // 每次选择只命中第一条匹配规则
            }
        }

        private void OnMonologueEnded(RealityMonologueEndedEvent evt)
        {
            foreach (var rule in _rules)
            {
                if (rule.source != ChoiceRule.Source.MonologueEnd) continue;
                if (rule.contentId != evt.monologueId) continue;

                ApplyRule(rule);
                return;
            }
        }

        private void ApplyRule(ChoiceRule rule)
        {
            bool newlySet = ChoiceFlagRegistry.SetFlag(rule.flag, rule.flagName);
            if (!newlySet) return; // 旗标已存在：不重复计情感权重

            _emotionalScore += rule.emotionalWeight;
            Debug.Log($"[RealityChoiceDirector] 旗标 {rule.flagName}(0x{rule.flag:X}) → 情感 {EmotionalProgress:F2}");
        }

        private void OnFlagsChanged(ChoiceFlagsChangedEvent evt)
        {
            ApplyToneAndVariants();
        }

        private void ApplyToneAndVariants()
        {
            float progress = EmotionalProgress;

            // 联动 T18：连续情感插值（Cold ←→ Warm）
            SceneToneManager.Instance.SetEmotionalProgress(progress, _toneDuration);

            // 场景变体三档：[0,1/3) Cold / [1/3,2/3) Neutral / [2/3,1] Warm
            var current = progress < 1f / 3f ? ToneVariant.Cold
                : progress < 2f / 3f ? ToneVariant.Neutral
                : ToneVariant.Warm;

            foreach (var group in _variantGroups)
            {
                bool active = group.variant == current;
                foreach (var obj in group.objects)
                {
                    if (obj != null && obj.activeSelf != active)
                        obj.SetActive(active);
                }
            }
        }
    }
}
