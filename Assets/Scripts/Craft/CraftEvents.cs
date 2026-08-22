using System;
using PastryWorld.Core;

namespace PastryWorld.Craft
{
    /// <summary>
    /// 制作步骤开始事件。
    /// </summary>
    public struct CraftStepStartedEvent
    {
        public string stepId;
        public string displayName;
    }

    /// <summary>
    /// 制作步骤完成事件。
    /// </summary>
    public struct CraftStepCompletedEvent
    {
        public string stepId;
        public float qualityScore;
        public bool success;
    }

    /// <summary>
    /// 整个制作流程完成事件。
    /// </summary>
    public struct CraftFlowCompletedEvent
    {
        /// <summary>配方 ID（T8 熟练度记录用）。</summary>
        public string recipeId;
        public float overallQuality;
        public bool allStepsSuccess;
        public int totalSteps;
        public int successSteps;
    }

    /// <summary>
    /// 自适应引导模式开关事件（T7）。
    /// 连续失败 ≥ 4 次激活，成功后解除。玩家无感知（纯后台逻辑），
    /// 视觉表现为世界层微光提示，不在 UI 显示。
    /// </summary>
    public struct AdaptiveGuidanceEvent
    {
        public bool active;
        public int consecutiveFailures;
    }
}
