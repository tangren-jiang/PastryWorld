using System;
using System.Collections.Generic;
using UnityEngine;
using PastryWorld.Core;
using PastryWorld.Craft;
using PastryWorld.Narrative;
using PastryWorld.Reality;

namespace PastryWorld.Integration
{
    /// <summary>
    /// Demo 流程状态（M6 系统集成）。
    /// </summary>
    public enum DemoFlowState
    {
        Initial,
        IntroDialogue,    // B1：NPC 引见
        Crafting,         // B2：制作点心
        GiftDialogue,     // B3：奉上点心
        MemoryPlayback,   // B4：记忆片段
        EndingDialogue,   // B5：收束
        Completed         // 节拍链结束
    }

    /// <summary>
    /// 节拍-记忆片段绑定（Memory 节拍自动触发播放）。
    /// </summary>
    [Serializable]
    public class MemoryBeatBinding
    {
        public string beatId;
        public MemoryClipData clip;
    }

    /// <summary>
    /// M6 系统集成核心：四线合流胶水。
    /// 将探索→制作→叙事→图鉴四条独立管线串联成完整 Demo 流程。
    ///
    /// 胶水职责：
    /// 1. CraftFlowCompletedEvent → SimpleInventory 入袋 + 节拍推进（Action 节拍）
    /// 2. BeatStartedEvent(Memory) → MemoryClipPlayer 播放
    /// 3. MemoryClipCompletedEvent → 节拍推进（Memory 节拍）
    /// 4. BeatChainCompletedEvent → Demo 流程结束
    ///
    /// 已有自动联动（无需此控制器）：
    /// - BeatStartedEvent(Dialogue) → DialogueManager 自动播放绑定对话 → 对话结束自动 Advance
    /// - CraftFlowCompletedEvent / GiftPresentedEvent / BeatStartedEvent / DialogueNodeEnteredEvent → CodexManager 自动解锁
    /// - DialogueChoiceMadeEvent / RealityMonologueEndedEvent → RealityChoiceDirector 旗标 + 色调
    /// </summary>
    public class DemoFlowController : MonoBehaviour
    {
        [Header("管理器引用")]
        [SerializeField] private BeatManager _beatManager;
        [SerializeField] CraftManager _craftManager;
        [SerializeField] private MemoryClipPlayer _memoryPlayer;

        [Header("记忆节拍绑定")]
        [Tooltip("Memory 类型节拍进入时自动播放对应记忆片段")]
        [SerializeField] private List<MemoryBeatBinding> _memoryBindings = new();

        [Header("制作→背包映射")]
        [Tooltip("制作完成后放入背包的点心 ID（与 DialogueNode.giftPastryId 对应）")]
        [SerializeField] private string _craftToPastryId = "honey_cake";
        [Tooltip("背包中显示的点心名称")]
        [SerializeField] private string _craftToDisplayName = "桂花蜂蜜糕";

        private IEventBus _eventBus;
        private DemoFlowState _state = DemoFlowState.Initial;

        /// <summary>当前流程状态。</summary>
        public DemoFlowState State => _state;

        /// <summary>当前节拍 ID（便捷查询）。</summary>
        public string CurrentBeatId => _beatManager != null ? _beatManager.CurrentBeatId : null;

        void Awake()
        {
            _eventBus = EventBus.Default;
        }

        void OnEnable()
        {
            _eventBus.Subscribe<BeatStartedEvent>(OnBeatStarted);
            _eventBus.Subscribe<CraftFlowCompletedEvent>(OnCraftCompleted);
            _eventBus.Subscribe<MemoryClipCompletedEvent>(OnMemoryCompleted);
            _eventBus.Subscribe<BeatChainCompletedEvent>(OnBeatChainCompleted);
        }

        void OnDisable()
        {
            _eventBus.Unsubscribe<BeatStartedEvent>(OnBeatStarted);
            _eventBus.Unsubscribe<CraftFlowCompletedEvent>(OnCraftCompleted);
            _eventBus.Unsubscribe<MemoryClipCompletedEvent>(OnMemoryCompleted);
            _eventBus.Unsubscribe<BeatChainCompletedEvent>(OnBeatChainCompleted);
        }

        /// <summary>
        /// 启动 Demo 流程：开启节拍链。
        /// B1(Dialogue)→B2(Action)→B3(Dialogue)→B4(Memory)→B5(Dialogue)
        /// </summary>
        public void StartDemo()
        {
            if (_beatManager == null)
            {
                Debug.LogError("[DemoFlowController] BeatManager 未引用");
                return;
            }

            _state = DemoFlowState.IntroDialogue;
            _beatManager.StartChain();
            Debug.Log("[DemoFlowController] Demo 流程启动");
        }

        // ---- 胶水 1：制作完成 → 入袋 + 推进节拍 ----

        private void OnCraftCompleted(CraftFlowCompletedEvent evt)
        {
            // 入袋（供后续对话"奉上点心"消费）
            SimpleInventory.Add(_craftToPastryId, _craftToDisplayName, evt.overallQuality);
            Debug.Log($"[DemoFlowController] 制作完成入袋: {_craftToDisplayName}（品质 {evt.overallQuality:F2}）");

            // Action 节拍 → 推进
            if (_beatManager != null && _beatManager.CurrentBeat != null
                && _beatManager.CurrentBeat.beatType == BeatType.Action)
            {
                _state = DemoFlowState.GiftDialogue;
                _beatManager.Advance();
            }
        }

        // ---- 胶水 2：Memory 节拍 → 自动播放记忆 ----

        private void OnBeatStarted(BeatStartedEvent evt)
        {
            if (evt.beatType == BeatType.Memory)
            {
                var binding = _memoryBindings.Find(b => b.beatId == evt.beatId);
                if (binding != null && binding.clip != null && _memoryPlayer != null)
                {
                    _state = DemoFlowState.MemoryPlayback;
                    _memoryPlayer.Play(binding.clip);
                    Debug.Log($"[DemoFlowController] 播放记忆: {binding.clip.clipId}");
                }
                else
                {
                    Debug.LogWarning($"[DemoFlowController] Memory 节拍 {evt.beatId} 无绑定片段，跳过");
                    _beatManager?.Advance();
                }
            }
            else if (evt.beatType == BeatType.Action)
            {
                _state = DemoFlowState.Crafting;
            }
            else if (evt.beatType == BeatType.Dialogue)
            {
                // 对话节拍由 DialogueManager 自动播放，状态更新仅作标记
                if (_state == DemoFlowState.MemoryPlayback || _state == DemoFlowState.GiftDialogue)
                    _state = DemoFlowState.EndingDialogue;
            }
        }

        // ---- 胶水 3：记忆播完 → 推进节拍 ----

        private void OnMemoryCompleted(MemoryClipCompletedEvent evt)
        {
            if (_beatManager != null && _beatManager.CurrentBeat != null
                && _beatManager.CurrentBeat.beatType == BeatType.Memory)
            {
                _beatManager.Advance();
            }
        }

        // ---- 胶水 4：节拍链结束 → Demo 完成 ----

        private void OnBeatChainCompleted(BeatChainCompletedEvent evt)
        {
            _state = DemoFlowState.Completed;
            Debug.Log("[DemoFlowController] Demo 流程完成！");

            // 色调收束：根据旗标情感进度调整
            var director = FindFirstObjectByType<RealityChoiceDirector>();
            if (director != null)
            {
                float progress = director.EmotionalProgress;
                SceneToneManager.Instance?.SetEmotionalProgress(progress);
                Debug.Log($"[DemoFlowController] 收束色调: 情感进度 {progress:F2}");
            }
        }
    }
}
