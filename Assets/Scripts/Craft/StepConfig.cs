using UnityEngine;

namespace PastryWorld.Craft
{
    /// <summary>
    /// 制作步骤配置 ScriptableObject。
    /// 数据驱动：目标值、容错区间、判定权重等。
    /// 技术预判报告 T1。
    /// </summary>
    [CreateAssetMenu(fileName = "StepConfig_", menuName = "PastryWorld/StepConfig")]
    public class StepConfig : ScriptableObject
    {
        [Header("基本信息")]
        public string stepId;
        public string displayName;
        [TextArea] public string description;

        [Header("判定参数")]
        [Tooltip("目标值（如目标重量、目标圆数等）")]
        public float targetValue = 100f;
        [Tooltip("容错区间（偏离目标的允许范围）")]
        public float tolerance = 10f;
        [Tooltip("通过阈值（品质分数 ≥ 此值则成功）")]
        [Range(0f, 1f)] public float passThreshold = 0.6f;
        [Tooltip("步骤时长上限（秒），超时自动判定")]
        public float duration = 30f;

        [Header("权重")]
        [Tooltip("此步骤在整体品质中的权重")]
        [Range(0f, 1f)] public float qualityWeight = 1f;

        [Header("输入参数")]
        [Tooltip("输入灵敏度/速度系数")]
        public float inputSensitivity = 1f;
        [Tooltip("最小输入量（如最少拖拽距离）")]
        public float minInput = 0.1f;
    }
}
