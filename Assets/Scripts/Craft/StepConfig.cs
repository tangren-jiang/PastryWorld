using UnityEngine;

namespace PastryWorld.Craft
{
    /// <summary>
    /// 制作步骤配置 ScriptableObject 模板。
    /// </summary>
    [CreateAssetMenu(fileName = "StepConfig_", menuName = "PastryWorld/StepConfig")]
    public class StepConfig : ScriptableObject
    {
        [Header("基本信息")]
        public string stepId;
        public string displayName;
        public string description;

        [Header("参数")]
        public float tolerance = 0.1f;
        public float duration = 5f;
    }
}
