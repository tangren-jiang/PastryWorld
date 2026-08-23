namespace PastryWorld.Narrative
{
    /// <summary>对话开始事件。</summary>
    public struct DialogueStartedEvent
    {
        public string dialogueId;
    }

    /// <summary>进入对话节点事件。</summary>
    public struct DialogueNodeEnteredEvent
    {
        public string dialogueId;
        public string nodeId;
        public string speakerName;
    }

    /// <summary>玩家做出分支选择事件。</summary>
    public struct DialogueChoiceMadeEvent
    {
        public string dialogueId;
        public string nodeId;
        public int choiceIndex;
        public string choiceText;
    }

    /// <summary>奉上点心事件。T21 图鉴/叙事系统的解锁触发点之一。</summary>
    public struct GiftPresentedEvent
    {
        public string dialogueId;
        public string nodeId;
        public string pastryId;
    }

    /// <summary>对话结束事件。</summary>
    public struct DialogueEndedEvent
    {
        public string dialogueId;
    }

    /// <summary>
    /// 环境变化事件（T14 envTrigger → EventBus → 消费方响应）。
    /// 如灯光变亮、天色变化（T18 SceneToneManager 可订阅）。
    /// </summary>
    public struct EnvironmentChangedEvent
    {
        public string triggerId;
    }
}
