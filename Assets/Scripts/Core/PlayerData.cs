using System.Collections.Generic;

namespace PastryWorld.Core
{
    /// <summary>
    /// 单个配方的熟练度数据。技术预判报告 T8：仅数据结构预留，不实装升级逻辑。
    /// Demo 中 masteryLevel 恒为 0，perfectCount 可记录但不触发任何功能。
    /// </summary>
    [System.Serializable]
    public class RecipeMastery
    {
        public string recipeId;

        /// <summary>熟练度等级 0-3。Demo 恒为 0（升级逻辑后置）。</summary>
        public int masteryLevel;

        /// <summary>完美完成次数（总品质 ≥ 0.9）。</summary>
        public int perfectCount;

        /// <summary>总制作次数。</summary>
        public int totalCount;

        /// <summary>历史最佳总品质分。</summary>
        public float bestScore;

        /// <summary>已解锁的配方变体 ID 列表（后置功能）。</summary>
        public List<string> unlockedVariations = new List<string>();

        public RecipeMastery(string id)
        {
            recipeId = id;
        }
    }

    /// <summary>
    /// 玩家数据容器。T8 仅包含配方熟练度字典；
    /// 存档读写由 T15 通过 ISaveSystem 接入（字典序列化在 T15 处理）。
    /// </summary>
    [System.Serializable]
    public class PlayerData
    {
        /// <summary>配方 ID → 熟练度数据。</summary>
        public Dictionary<string, RecipeMastery> recipeMastery = new Dictionary<string, RecipeMastery>();

        /// <summary>
        /// 获取指定配方的熟练度数据（不存在则创建默认条目）。
        /// </summary>
        public RecipeMastery GetMastery(string recipeId)
        {
            if (string.IsNullOrEmpty(recipeId)) return null;

            if (!recipeMastery.TryGetValue(recipeId, out var mastery))
            {
                mastery = new RecipeMastery(recipeId);
                recipeMastery[recipeId] = mastery;
            }
            return mastery;
        }
    }
}
