using System;
using System.Collections.Generic;
using UnityEngine;

namespace PastryWorld.Core
{
    /// <summary>
    /// 点心条目（简化背包的值类型）。
    /// </summary>
    [Serializable]
    public class PastryItem
    {
        public string pastryId;
        public string displayName;

        /// <summary>制作品质 0-1（来自 CraftFlowCompletedEvent，供后续叙事分支使用）</summary>
        public float quality;

        public PastryItem() { }

        public PastryItem(string id, string displayName, float quality = 0f)
        {
            pastryId = id;
            this.displayName = displayName;
            this.quality = quality;
        }
    }

    /// <summary>
    /// 简化背包（T14 对齐修订）：Dictionary&lt;string, PastryItem&gt;，
    /// 不需要完整物品栏 UI——唯一消费场景是对话"奉上点心"按钮判断。
    /// 后续存档（T15 后）可整体序列化字典。
    /// </summary>
    public static class SimpleInventory
    {
        private static readonly Dictionary<string, PastryItem> _items =
            new Dictionary<string, PastryItem>();

        /// <summary>背包内容变化（增/删）。Demo 内暂无订阅者，预留。</summary>
        public static event Action Changed;

        public static bool Has(string pastryId) =>
            !string.IsNullOrEmpty(pastryId) && _items.ContainsKey(pastryId);

        public static PastryItem Get(string pastryId)
        {
            _items.TryGetValue(pastryId, out var item);
            return item;
        }

        public static void Add(PastryItem item)
        {
            if (item == null || string.IsNullOrEmpty(item.pastryId)) return;
            _items[item.pastryId] = item;
            Changed?.Invoke();
        }

        public static void Add(string pastryId, string displayName, float quality = 0f) =>
            Add(new PastryItem(pastryId, displayName, quality));

        /// <summary>
        /// 移除并返回条目（"奉上"消耗点心）。背包无此物品返回 null。
        /// </summary>
        public static PastryItem Take(string pastryId)
        {
            if (string.IsNullOrEmpty(pastryId) || !_items.TryGetValue(pastryId, out var item))
                return null;

            _items.Remove(pastryId);
            Changed?.Invoke();
            return item;
        }

        /// <summary>清空（重开/测试用）。</summary>
        public static void Clear()
        {
            _items.Clear();
            Changed?.Invoke();
        }
    }
}
