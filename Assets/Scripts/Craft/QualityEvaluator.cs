using System.Collections.Generic;
using UnityEngine;
using PastryWorld.Core;

namespace PastryWorld.Craft
{
    /// <summary>
    /// 品质判定器。
    /// T1 骨架：简化距离判定。
    /// T4 增强：Fréchet Distance 轨迹比较 + 调参支持。
    /// 技术预判报告 T1（骨架）+ T4（算法）。
    /// </summary>
    public class QualityEvaluator : MonoBehaviour
    {
        [Header("Fréchet 参数")]
        [Tooltip("最大允许距离（超过则分数为0）")]
        [SerializeField] private float _maxAllowableDistance = 100f;
        [Tooltip("降采样上限（性能优化）")]
        [SerializeField] private int _maxSamples = 64;

        [Header("T7 自适应难度")]
        [Tooltip("自适应难度组件（为空时自动查找场景中的 AdaptiveDifficulty）")]
        [SerializeField] private AdaptiveDifficulty _adaptive;
        [Tooltip("容错扩大时的分数上限——锁定'完美'档（90+）不被自适应影响")]
        [SerializeField, Range(0.5f, 1f)] private float _adaptiveScoreCap = 0.9f;

        private void Awake()
        {
            if (_adaptive == null)
                _adaptive = FindObjectOfType<AdaptiveDifficulty>();
        }

        /// <summary>
        /// 注入自适应难度组件（测试或显式装配用）。
        /// </summary>
        public void SetAdaptive(AdaptiveDifficulty adaptive)
        {
            _adaptive = adaptive;
        }

        /// <summary>
        /// 评估数值型输入（称量/蒸制等）。
        /// T7：应用自适应容错。规范：容错扩大只在"良好"档以下生效——
        /// 提分封顶于 _adaptiveScoreCap（不进入完美档），但不压低原有完美分数。
        /// </summary>
        public virtual float Evaluate(float actualValue, float targetValue, float tolerance)
        {
            // 零容差语义 = 必须完全命中（而非任意输入满分）
            if (tolerance <= 0f)
                return Mathf.Abs(actualValue - targetValue) < Mathf.Epsilon ? 1f : 0f;

            float diff = Mathf.Abs(actualValue - targetValue);
            float baseScore = Mathf.Clamp01(1f - diff / tolerance);

            if (_adaptive == null) return baseScore;

            float effectiveTolerance = _adaptive.ApplyTolerance(tolerance);
            if (effectiveTolerance <= tolerance) return baseScore;

            float expandedScore = Mathf.Clamp01(1f - diff / effectiveTolerance);
            return Mathf.Min(expandedScore, Mathf.Max(baseScore, _adaptiveScoreCap));
        }

        /// <summary>
        /// 简化评估：直接返回 0-1 分数。
        /// </summary>
        public virtual float EvaluateRaw(float normalizedValue)
        {
            return Mathf.Clamp01(normalizedValue);
        }

        /// <summary>
        /// 轨迹评估：用 Fréchet Distance 比较玩家轨迹与理想轨迹。
        /// T4 新增。
        /// </summary>
        /// <param name="playerTrajectory">玩家输入轨迹</param>
        /// <param name="idealTrajectory">理想轨迹</param>
        public virtual float EvaluateTrajectory(List<Vector2> playerTrajectory, List<Vector2> idealTrajectory)
        {
            if (playerTrajectory == null || idealTrajectory == null
                || playerTrajectory.Count == 0 || idealTrajectory.Count == 0)
            {
                Debug.LogWarning("[QualityEvaluator] 轨迹为空，返回 0 分");
                return 0f;
            }

            float distance = FrechetDistance.CalculateFast(
                playerTrajectory, idealTrajectory, _maxSamples);

            return EvaluateDistanceScore(distance, _maxAllowableDistance);
        }

        /// <summary>
        /// 归一化轨迹评估：先对齐两条轨迹的起点和终点，再计算 Fréchet Distance。
        /// 适用于"形状对了但位置偏了"的情况。
        /// </summary>
        public virtual float EvaluateTrajectoryNormalized(List<Vector2> playerTrajectory, List<Vector2> idealTrajectory)
        {
            if (playerTrajectory == null || idealTrajectory == null
                || playerTrajectory.Count < 2 || idealTrajectory.Count < 2)
                return 0f;

            // 归一化：将两条轨迹缩放到相同的包围盒
            var normalizedPlayer = NormalizeTrajectory(playerTrajectory);
            var normalizedIdeal = NormalizeTrajectory(idealTrajectory);

            float distance = FrechetDistance.CalculateFast(
                normalizedPlayer, normalizedIdeal, _maxSamples);

            return EvaluateDistanceScore(distance, _maxAllowableDistance);
        }

        /// <summary>
        /// 距离 → 分数，应用 T7 自适应容错。
        /// 提分封顶于 _adaptiveScoreCap（不进入完美档），但不压低原有完美分数。
        /// </summary>
        private float EvaluateDistanceScore(float distance, float baseMaxDistance)
        {
            if (baseMaxDistance <= 0f) return 0f;

            float baseScore = FrechetDistance.DistanceToScore(distance, baseMaxDistance);

            if (_adaptive == null) return baseScore;

            float maxDistance = _adaptive.ApplyTolerance(baseMaxDistance);
            if (maxDistance <= baseMaxDistance) return baseScore;

            float expandedScore = FrechetDistance.DistanceToScore(distance, maxDistance);
            return Mathf.Min(expandedScore, Mathf.Max(baseScore, _adaptiveScoreCap));
        }

        /// <summary>
        /// 将轨迹归一化到 [0,1] x [0,1] 空间。
        /// </summary>
        private List<Vector2> NormalizeTrajectory(List<Vector2> trajectory)
        {
            if (trajectory.Count == 0) return trajectory;

            // 计算包围盒
            Vector2 min = trajectory[0];
            Vector2 max = trajectory[0];
            foreach (var p in trajectory)
            {
                min = Vector2.Min(min, p);
                max = Vector2.Max(max, p);
            }

            Vector2 size = max - min;
            float maxDim = Mathf.Max(size.x, size.y);
            if (maxDim <= 0.01f) maxDim = 1f; // 防止除零

            // 归一化
            var result = new List<Vector2>(trajectory.Count);
            foreach (var p in trajectory)
            {
                result.Add((p - min) / maxDim);
            }
            return result;
        }

        public float MaxAllowableDistance => _maxAllowableDistance;
    }
}
