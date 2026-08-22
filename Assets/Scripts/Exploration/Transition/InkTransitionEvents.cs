namespace PastryWorld.Exploration
{
    /// <summary>
    /// 水墨转场开始事件（T12）。
    /// cover=true 覆盖阶段（墨迹扩散遮蔽屏幕），false 揭示阶段。
    /// </summary>
    public struct InkTransitionStartedEvent
    {
        public bool cover;
        public float duration;
    }

    /// <summary>
    /// 水墨转场全部完成事件（T12）。此时屏幕已恢复可见。
    /// </summary>
    public struct InkTransitionCompletedEvent
    {
        public float totalDuration;
    }
}
