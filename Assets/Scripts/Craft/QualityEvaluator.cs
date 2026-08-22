using UnityEngine;
using PastryWorld.Core;

namespace PastryWorld.Craft
{
    /// <summary>
    /// 品质判定器。骨架实现，Fréchet Distance 算法在 T4 实现。
    /// 技术预判报告 T1（骨架）+ T4（算法）。
    /// </summary>
    public class QualityEvaluator : MonoBehaviour
    {
        /// <summary>
        /// 评估输入数据与目标的匹配度，返回 0-1 品质分数。
        /// </summary>
        /// <param name="actualValue">玩家实际值</param>
        /// <param name="targetValue">目标值</param>
        /// <param name="tolerance">容错区间</param>
        public virtual float Evaluate(float actualValue, float targetValue, float tolerance)
        {
            if (tolerance <= 0f) return 1f;

            float diff = Mathf.Abs(actualValue - targetValue);
            float score = 1f - (diff / tolerance);
            return Mathf.Clamp01(score);
        }

        /// <summary>
        /// 简化评估：直接返回 0-1 分数。
        /// T4 将重写为 Fréchet Distance 算法。
        /// </summary>
        public virtual float EvaluateRaw(float normalizedValue)
        {
            return Mathf.Clamp01(normalizedValue);
        }
    }
}
