using UnityEngine;

namespace PastryWorld.Codex
{
    /// <summary>
    /// 图鉴品质等级。GDD-05 定义。
    /// </summary>
    public enum CodexQuality
    {
        Bronze,
        Silver,
        Gold
    }

    /// <summary>
    /// 图鉴条目 ScriptableObject 模板。GDD-05 T21。
    /// </summary>
    [CreateAssetMenu(fileName = "CodexEntry_", menuName = "PastryWorld/CodexEntry")]
    public class CodexEntrySO : ScriptableObject
    {
        [Header("条目信息")]
        public string entryId;
        public string displayName;
        public CodexQuality quality;

        [Header("内容")]
        [TextArea] public string description;
        public Sprite illustration;

        [Header("交叉索引")]
        public string[] relatedEntryIds;
    }
}
