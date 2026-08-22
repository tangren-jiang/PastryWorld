namespace PastryWorld.Narrative
{
    /// <summary>
    /// 节拍开始事件。当 BeatManager 进入新节拍时发布。
    /// </summary>
    public struct BeatStartedEvent
    {
        public string beatId;
        public BeatType beatType;
        public string dialogueText;
    }

    /// <summary>
    /// 节拍完成事件。当节拍逻辑执行完毕时发布。
    /// </summary>
    public struct BeatCompletedEvent
    {
        public string beatId;
    }

    /// <summary>
    /// 节拍链结束事件。当没有下一个节拍时发布。
    /// </summary>
    public struct BeatChainCompletedEvent
    {
        public string lastBeatId;
    }
}
