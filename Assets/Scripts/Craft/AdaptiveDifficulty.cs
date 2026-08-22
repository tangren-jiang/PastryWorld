using UnityEngine;
using PastryWorld.Core;

namespace PastryWorld.Craft
{
    /// <summary>
    /// 连续失败自适应难度（T7）。
    /// 静默记录连续失败次数并扩大容错区间，玩家无感知。
    /// 技术预判报告 T7 规范：
    /// - 容错 = 基础容错 × (1 + 连续失败 × 0.1)，上限 1.5 倍
    /// - 容错扩大只在"良好"档以下生效（完美档由 QualityEvaluator 锁定分数上限）
    /// - 连续失败 ≥ 4 次激活引导模式（发布事件，视觉表现为世界层微光提示）
    /// 关键：纯后台逻辑，不在 UI 显示。
    /// </summary>
    public class AdaptiveDifficulty : MonoBehaviour
    {
        [Header("自适应参数")]
        [Tooltip("低于此分数（0-1）计为一次失败")]
        [SerializeField, Range(0f, 1f)] private float _failureScoreThreshold = 0.6f;
        [Tooltip("每次连续失败增加的容错比例")]
        [SerializeField] private float _toleranceGainPerFailure = 0.1f;
        [Tooltip("容错倍率上限（不超过基础值的此倍数）")]
        [SerializeField] private float _maxToleranceMultiplier = 1.5f;
        [Tooltip("连续失败达到此次数后激活引导模式")]
        [SerializeField] private int _guidanceThreshold = 4;

        /// <summary>当前连续失败次数</summary>
        public int ConsecutiveFailures { get; private set; }

        /// <summary>当前容错倍率（1.0 ~ _maxToleranceMultiplier）</summary>
        public float CurrentToleranceMultiplier =>
            Mathf.Min(1f + ConsecutiveFailures * _toleranceGainPerFailure, _maxToleranceMultiplier);

        /// <summary>引导模式是否激活（连续失败 ≥ 阈值）</summary>
        public bool IsGuidanceActive { get; private set; }

        /// <summary>
        /// 应用自适应容错。返回调整后的容错区间。
        /// </summary>
        public float ApplyTolerance(float baseTolerance)
        {
            return baseTolerance * CurrentToleranceMultiplier;
        }

        /// <summary>
        /// 步骤完成时反馈结果。
        /// 失败累计计数；成功递减一次（不直接清零，避免"失败-成功-失败"锯齿抖动）。
        /// </summary>
        public void OnStepResult(float score)
        {
            if (score < _failureScoreThreshold)
                ConsecutiveFailures++;
            else
                ConsecutiveFailures = Mathf.Max(0, ConsecutiveFailures - 1);

            UpdateGuidanceState();
        }

        /// <summary>
        /// 重置状态（新一轮制作流程开始时由 CraftManager 调用）。
        /// </summary>
        public void ResetState()
        {
            ConsecutiveFailures = 0;
            UpdateGuidanceState();
        }

        private void UpdateGuidanceState()
        {
            bool shouldActivate = ConsecutiveFailures >= _guidanceThreshold;
            if (shouldActivate == IsGuidanceActive) return;

            IsGuidanceActive = shouldActivate;
            PastryWorld.Core.EventBus.Default.Publish(new AdaptiveGuidanceEvent
            {
                active = IsGuidanceActive,
                consecutiveFailures = ConsecutiveFailures
            });
        }
    }
}
