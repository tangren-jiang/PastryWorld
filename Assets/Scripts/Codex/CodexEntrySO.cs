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
    /// 图鉴条目分类（手账分区）。
    /// </summary>
    public enum CodexCategory
    {
        Place,       // 地点
        Pastry,      // 点心
        Ingredient,  // 原料
        Character,   // 人物
        Memory       // 记忆
    }

    /// <summary>
    /// 解锁触发来源。T21 依赖 T1（制作完成）+ T13（节拍/对话）双触发。
    /// </summary>
    public enum CodexUnlockSource
    {
        Manual,         // 手动/初始解锁
        Craft,          // CraftFlowCompletedEvent.recipeId
        Gift,           // GiftPresentedEvent.pastryId
        Beat,           // BeatStartedEvent.beatId
        DialogueNode    // DialogueNodeEnteredEvent.nodeId
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
        public CodexCategory category;

        [Header("内容")]
        [TextArea] public string description;
        public Sprite illustration;

        [Header("解锁条件")]
        [Tooltip("解锁触发来源")]
        public CodexUnlockSource unlockSource = CodexUnlockSource.Manual;
        [Tooltip("来源 ID（配方 ID / 点心 ID / 节拍 ID / 节点 ID），Manual 源忽略")]
        public string sourceId = "";
        [Tooltip("初始即解锁（如手账来历这类引导条目）")]
        public bool startUnlocked = false;

        [Header("交叉索引")]
        public string[] relatedEntryIds;

        /// <summary>分类中文名（UI 显示用）。</summary>
        public static string CategoryName(CodexCategory c)
        {
            return c switch
            {
                CodexCategory.Place => "地点",
                CodexCategory.Pastry => "点心",
                CodexCategory.Ingredient => "原料",
                CodexCategory.Character => "人物",
                CodexCategory.Memory => "记忆",
                _ => c.ToString()
            };
        }

        /// <summary>品质显示色（UI 徽标用）。</summary>
        public static Color QualityColor(CodexQuality q)
        {
            return q switch
            {
                CodexQuality.Bronze => new Color(0.69f, 0.55f, 0.34f),
                CodexQuality.Silver => new Color(0.75f, 0.78f, 0.82f),
                CodexQuality.Gold => new Color(0.83f, 0.69f, 0.22f),
                _ => Color.white
            };
        }
    }
}
