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
        public float overallQuality;
        public bool allStepsSuccess;
        public int totalSteps;
        public int successSteps;
    }
}
