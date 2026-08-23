namespace PastryWorld.Reality
{
    /// <summary>现实独白开始事件（T17）。</summary>
    public struct RealityMonologueStartedEvent
    {
        public string monologueId;
    }

    /// <summary>现实独白播完事件（T20 规则触发点之一：独白读完记旗标）。</summary>
    public struct RealityMonologueEndedEvent
    {
        public string monologueId;
    }

    /// <summary>选择旗标变化事件（T20）。changedFlag 为本次新置位的位。</summary>
    public struct ChoiceFlagsChangedEvent
    {
        public int changedFlag;
        public string flagName;
        public int allFlags;
    }
}
