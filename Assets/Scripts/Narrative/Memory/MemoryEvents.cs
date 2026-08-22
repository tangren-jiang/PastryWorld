namespace PastryWorld.Narrative
{
    /// <summary>
    /// 记忆片段开始播放事件。
    /// </summary>
    public struct MemoryClipStartedEvent
    {
        public string clipId;
        public MemoryPlayMode playMode;
    }

    /// <summary>
    /// 记忆片段播放完成事件（淡出结束后发布）。
    /// </summary>
    public struct MemoryClipCompletedEvent
    {
        public string clipId;
        public MemoryPlayMode playMode;
    }
}
