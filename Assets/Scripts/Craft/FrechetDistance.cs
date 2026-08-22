using System.Collections.Generic;
using UnityEngine;

namespace PastryWorld.Craft
{
    /// <summary>
    /// Fréchet Distance 算法。比较两条曲线的相似度。
    /// 技术预判报告 T4。
    /// 离散 Fréchet Distance：O(n*m) 动态规划。
    /// </summary>
    public static class FrechetDistance
    {
        /// <summary>
        /// 计算离散 Fréchet Distance。
        /// 返回值越小表示两条曲线越相似。
        /// </summary>
        public static float Calculate(List<Vector2> curveA, List<Vector2> curveB)
        {
            if (curveA == null || curveB == null || curveA.Count == 0 || curveB.Count == 0)
                return float.MaxValue;

            int n = curveA.Count;
            int m = curveB.Count;

            // DP 表
            float[,] dp = new float[n, m];

            // 初始化
            dp[0, 0] = Vector2.Distance(curveA[0], curveB[0]);

            // 第一行
            for (int j = 1; j < m; j++)
            {
                dp[0, j] = Mathf.Max(dp[0, j - 1], Vector2.Distance(curveA[0], curveB[j]));
            }

            // 第一列
            for (int i = 1; i < n; i++)
            {
                dp[i, 0] = Mathf.Max(dp[i - 1, 0], Vector2.Distance(curveA[i], curveB[0]));
            }

            // 填充 DP 表
            for (int i = 1; i < n; i++)
            {
                for (int j = 1; j < m; j++)
                {
                    float dist = Vector2.Distance(curveA[i], curveB[j]);
                    dp[i, j] = Mathf.Max(
                        Mathf.Min(
                            Mathf.Min(dp[i - 1, j], dp[i, j - 1]),
                            dp[i - 1, j - 1]
                        ),
                        dist
                    );
                }
            }

            return dp[n - 1, m - 1];
        }

        /// <summary>
        /// 计算品质分数（0-1）。距离越小分数越高。
        /// </summary>
        /// <param name="distance">Fréchet Distance 值</param>
        /// <param name="maxAllowableDistance">最大允许距离（超过则分数为0）</param>
        public static float DistanceToScore(float distance, float maxAllowableDistance)
        {
            if (maxAllowableDistance <= 0f) return 1f;
            return Mathf.Clamp01(1f - (distance / maxAllowableDistance));
        }

        /// <summary>
        /// 快速版本：降采样后计算。适用于长轨迹。
        /// </summary>
        public static float CalculateFast(List<Vector2> curveA, List<Vector2> curveB, int maxSamples = 64)
        {
            var sampledA = Downsample(curveA, maxSamples);
            var sampledB = Downsample(curveB, maxSamples);
            return Calculate(sampledA, sampledB);
        }

        /// <summary>
        /// 均匀降采样。
        /// </summary>
        private static List<Vector2> Downsample(List<Vector2> curve, int maxSamples)
        {
            if (curve == null || curve.Count <= maxSamples)
                return curve;

            var result = new List<Vector2>(maxSamples);
            float step = (float)(curve.Count - 1) / (maxSamples - 1);
            for (int i = 0; i < maxSamples; i++)
            {
                int idx = Mathf.RoundToInt(i * step);
                idx = Mathf.Clamp(idx, 0, curve.Count - 1);
                result.Add(curve[idx]);
            }
            return result;
        }
    }
}
