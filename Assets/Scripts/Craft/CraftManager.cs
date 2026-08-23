using System.Collections.Generic;
using UnityEngine;
using PastryWorld.Core;

namespace PastryWorld.Craft
{
    /// <summary>
    /// 制作管线管理器。管理工艺步骤序列，收集品质分数。
    /// 技术预判报告 T1。
    /// </summary>
    public class CraftManager : MonoBehaviour
    {
        [Header("制作步骤")]
        [SerializeField] private CraftStep[] _steps;
        [SerializeField] private bool _autoStart = false;
        [Tooltip("步骤间延迟（秒），等待反馈播放完成")]
        [SerializeField] private float _stepTransitionDelay = 0.5f;
        [Tooltip("当前配方 ID（T8 熟练度记录用，空则不记录）")]
        [SerializeField] private string _recipeId = "";
        [Tooltip("迷你制作模式（T19）：只执行标记为 IsMiniStep 的关键步骤")]
        [SerializeField] private bool _miniCraftMode = false;

        private IEventBus _eventBus;
        private AdaptiveDifficulty _adaptive;
        private int _currentIndex = -1;
        private readonly List<float> _qualityScores = new();
        private readonly List<CraftStep> _pipeline = new();
        private int _successCount = 0;

        public int CurrentIndex => _currentIndex;
        public int TotalSteps => _steps != null ? _steps.Length : 0;
        /// <summary>本轮实际执行的步骤数（迷你模式下少于 TotalSteps）。</summary>
        public int ExecutedStepCount => _pipeline.Count;
        /// <summary>迷你制作模式（T19）。</summary>
        public bool MiniCraftMode => _miniCraftMode;
        public bool IsRunning { get; private set; }
        public AdaptiveDifficulty Adaptive => _adaptive;

        void Awake()
        {
            _eventBus = PastryWorld.Core.EventBus.Default;
            _adaptive = FindObjectOfType<AdaptiveDifficulty>();
        }

        void Start()
        {
            if (_autoStart) StartCraft();
        }

        /// <summary>
        /// 启动制作流程。
        /// </summary>
        public void StartCraft()
        {
            if (IsRunning)
            {
                Debug.LogWarning("[CraftManager] 已在运行中");
                return;
            }

            if (_steps == null || _steps.Length == 0)
            {
                Debug.LogWarning("[CraftManager] 无步骤可执行");
                return;
            }

            IsRunning = true;
            _currentIndex = -1;
            _qualityScores.Clear();
            _successCount = 0;

            // T19：迷你模式过滤——只执行标记为 IsMiniStep 的关键步骤
            BuildPipeline();

            // T7：重置自适应难度（新一轮制作从基础容错开始）
            if (_adaptive != null) _adaptive.ResetState();

            // 重置所有步骤
            foreach (var step in _steps)
            {
                if (step != null) step.ResetStep();
            }

            AdvanceToNext();
        }

        /// <summary>启动迷你制作流程（T19）：只执行关键步骤。</summary>
        public void StartMiniCraft()
        {
            _miniCraftMode = true;
            StartCraft();
        }

        /// <summary>启动完整制作流程（T19）。</summary>
        public void StartFullCraft()
        {
            _miniCraftMode = false;
            StartCraft();
        }

        private void BuildPipeline()
        {
            _pipeline.Clear();
            foreach (var step in _steps)
            {
                if (step == null) continue;
                if (_miniCraftMode && !step.IsMiniStep) continue;
                _pipeline.Add(step);
            }

            // 兜底：迷你模式下没有任何标记步骤 → 回退完整流程
            if (_pipeline.Count == 0)
            {
                Debug.LogWarning("[CraftManager] 迷你模式无标记步骤，回退完整流程");
                foreach (var step in _steps)
                {
                    if (step != null) _pipeline.Add(step);
                }
            }

            if (_miniCraftMode)
                Debug.Log($"[CraftManager] 迷你模式: {_pipeline.Count}/{_steps.Length} 步 " +
                          $"[{string.Join(" → ", _pipeline.ConvertAll(s => s.StepId))}]");
        }

        /// <summary>
        /// 前进到下一步骤。
        /// </summary>
        public void AdvanceToNext()
        {
            if (!IsRunning) return;

            _currentIndex++;

            if (_currentIndex >= _pipeline.Count)
            {
                CompleteCraft();
                return;
            }

            var step = _pipeline[_currentIndex];
            if (step == null)
            {
                Debug.LogError($"[CraftManager] 步骤 {_currentIndex} 为空");
                AdvanceToNext();
                return;
            }

            // 订阅完成事件
            step.OnStepCompleted -= OnStepCompleted;
            step.OnStepCompleted += OnStepCompleted;

            // 发布开始事件
            _eventBus.Publish(new CraftStepStartedEvent
            {
                stepId = step.StepId,
                displayName = step.Config != null ? step.Config.displayName : step.StepId
            });

            step.BeginStep();
        }

        private void OnStepCompleted(CraftStep step, float quality)
        {
            step.OnStepCompleted -= OnStepCompleted;

            _qualityScores.Add(quality);
            if (step.LastResultSuccess) _successCount++;

            // T7：反馈结果给自适应难度（静默调整后续容错）
            if (_adaptive != null) _adaptive.OnStepResult(quality);

            _eventBus.Publish(new CraftStepCompletedEvent
            {
                stepId = step.StepId,
                qualityScore = quality,
                success = step.LastResultSuccess
            });

            // 延迟前进，等待反馈动画播放完成
            StartCoroutine(DelayedAdvance(_stepTransitionDelay));
        }

        private System.Collections.IEnumerator DelayedAdvance(float delay)
        {
            yield return new UnityEngine.WaitForSeconds(delay);
            AdvanceToNext();
        }

        private void CompleteCraft()
        {
            IsRunning = false;

            float overall = CalculateOverallQuality();
            bool allSuccess = _successCount == _pipeline.Count;

            _eventBus.Publish(new CraftFlowCompletedEvent
            {
                recipeId = _recipeId,
                overallQuality = overall,
                allStepsSuccess = allSuccess,
                totalSteps = _pipeline.Count,
                successSteps = _successCount
            });

            Debug.Log($"[CraftManager] 制作完成{(_miniCraftMode ? "（迷你）" : "")}: " +
                      $"总品质={overall:F2}, 成功 {_successCount}/{_pipeline.Count}");
        }

        private float CalculateOverallQuality()
        {
            if (_qualityScores.Count == 0) return 0f;

            // 加权平均（用 StepConfig.qualityWeight）
            float totalWeight = 0f;
            float weightedSum = 0f;

            for (int i = 0; i < _qualityScores.Count && i < _pipeline.Count; i++)
            {
                float weight = _pipeline[i].Config != null ? _pipeline[i].Config.qualityWeight : 1f;
                weightedSum += _qualityScores[i] * weight;
                totalWeight += weight;
            }

            return totalWeight > 0 ? weightedSum / totalWeight : 0f;
        }

        /// <summary>
        /// 获取当前 EventBus（供外部订阅事件）。
        /// </summary>
        public IEventBus EventBus => _eventBus;
    }
}
