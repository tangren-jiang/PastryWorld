using UnityEngine;

namespace PastryWorld.Exploration
{
    /// <summary>
    /// 玩家完成一次有效交互（与可交互物体/NPC 对话）。
    /// 有效交互会重置弱引导计时器（探索移动不算交互）。
    /// 技术预判报告 T10。
    /// </summary>
    public struct PlayerInteractedEvent
    {
        public Vector2 position;
        public string targetName;
    }

    /// <summary>
    /// 弱引导等级变化事件。
    /// L0=正常，L1=环境微光提示增强（Demo 只做 L0+L1，L2/L3 后置）。
    /// 所有引导效果都是环境/世界层面，消费方不得显示 UI 箭头/标记/弹窗。
    /// </summary>
    public struct GuidanceLevelChangedEvent
    {
        public int level;
        /// <summary>触发本次变化的无交互时长（秒）。</summary>
        public float idleTime;
    }
}
