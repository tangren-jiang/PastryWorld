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

        private IEventBus _eventBus;
        private int _currentIndex = -1;
        private readonly List<float> _qualityScores = new();
        private int _successCount = 0;

        public int CurrentIndex => _currentIndex;
        public int TotalSteps => _steps != null ? _steps.Length : 0;
        public bool IsRunning { get; private set; }

        void Awake()
        {
            _eventBus = new EventBus();
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

            // 重置所有步骤
            foreach (var step in _steps)
            {
                if (step != null) step.ResetStep();
            }

            AdvanceToNext();
        }

        /// <summary>
        /// 前进到下一步骤。
        /// </summary>
        public void AdvanceToNext()
        {
            if (!IsRunning) return;

            _currentIndex++;

            if (_currentIndex >= _steps.Length)
            {
                CompleteCraft();
                return;
            }

            var step = _steps[_currentIndex];
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

            _eventBus.Publish(new CraftStepCompletedEvent
            {
                stepId = step.StepId,
                qualityScore = quality,
                success = step.LastResultSuccess
            });

            // 延迟一小段后前进（等反馈播放完）
            // 简化版：立即前进。实际需等反馈动画完成。
            // TODO: 配合反馈时长等待
            AdvanceToNext();
        }

        private void CompleteCraft()
        {
            IsRunning = false;

            float overall = CalculateOverallQuality();
            bool allSuccess = _successCount == _steps.Length;

            _eventBus.Publish(new CraftFlowCompletedEvent
            {
                overallQuality = overall,
                allStepsSuccess = allSuccess,
                totalSteps = _steps.Length,
                successSteps = _successCount
            });

            Debug.Log($"[CraftManager] 制作完成: 总品质={overall:F2}, 成功 {_successCount}/{_steps.Length}");
        }

        private float CalculateOverallQuality()
        {
            if (_qualityScores.Count == 0) return 0f;

            // 加权平均（用 StepConfig.qualityWeight）
            float totalWeight = 0f;
            float weightedSum = 0f;

            for (int i = 0; i < _qualityScores.Count && i < _steps.Length; i++)
            {
                float weight = _steps[i].Config != null ? _steps[i].Config.qualityWeight : 1f;
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
