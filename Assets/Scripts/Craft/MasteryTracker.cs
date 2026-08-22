using UnityEngine;
using PastryWorld.Core;

namespace PastryWorld.Craft
{
    /// <summary>
    /// 熟练度记录器。技术预判报告 T8：仅记录统计，不实装升级逻辑。
    /// 监听 CraftFlowCompletedEvent，把制作结果写入 PlayerData.recipeMastery。
    /// Demo 中 masteryLevel 恒为 0，perfectCount 只记录不触发任何功能。
    /// 存档持久化由 T15 接入（当前数据仅存于内存）。
    /// </summary>
    public class MasteryTracker : MonoBehaviour
    {
        [Tooltip("完美档判定阈值（与品质档位一致：90+）")]
        [SerializeField, Range(0.5f, 1f)] private float _perfectThreshold = 0.9f;

        private IEventBus _eventBus;
        private PlayerData _playerData;

        /// <summary>
        /// 玩家数据（供 T15 存档系统读取/写入）。
        /// </summary>
        public PlayerData Data => _playerData;

        void Awake()
        {
            _eventBus = PastryWorld.Core.EventBus.Default;
            _playerData = new PlayerData();
        }

        void OnEnable()
        {
            _eventBus.Subscribe<CraftFlowCompletedEvent>(OnCraftFlowCompleted);
        }

        void OnDisable()
        {
            _eventBus.Unsubscribe<CraftFlowCompletedEvent>(OnCraftFlowCompleted);
        }

        private void OnCraftFlowCompleted(CraftFlowCompletedEvent evt)
        {
            // 无配方 ID 则不记录（如临时测试流程）
            if (string.IsNullOrEmpty(evt.recipeId)) return;

            RecipeMastery mastery = _playerData.GetMastery(evt.recipeId);

            mastery.totalCount++;
            if (evt.overallQuality >= _perfectThreshold) mastery.perfectCount++;
            if (evt.overallQuality > mastery.bestScore) mastery.bestScore = evt.overallQuality;

            // T8 预留：masteryLevel 恒为 0，升级逻辑（完美次数累计解锁变体）后置

            Debug.Log($"[MasteryTracker] {evt.recipeId}: 第 {mastery.totalCount} 次制作 " +
                      $"品质={evt.overallQuality:F2} 最佳={mastery.bestScore:F2} " +
                      $"完美={mastery.perfectCount} 等级={mastery.masteryLevel}");
        }
    }
}
