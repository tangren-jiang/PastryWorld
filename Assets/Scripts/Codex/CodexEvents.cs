namespace PastryWorld.Codex
{
    /// <summary>图鉴条目解锁事件。</summary>
    public struct CodexEntryUnlockedEvent
    {
        public string entryId;
        public CodexCategory category;
        public string displayName;
        public CodexQuality quality;
    }

    /// <summary>条目品质升级事件（重复制作只升不降）。</summary>
    public struct CodexQualityUpgradedEvent
    {
        public string entryId;
        public CodexQuality oldQuality;
        public CodexQuality newQuality;
    }

    /// <summary>图鉴完成度变化事件（T23）。</summary>
    public struct CodexCompletionChangedEvent
    {
        public int unlockedCount;
        public int totalCount;
        public float percent;
    }
}
