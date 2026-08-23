using System.Collections.Generic;
using UnityEngine;
using PastryWorld.Core;
using PastryWorld.Craft;
using PastryWorld.Narrative;

namespace PastryWorld.Codex
{
    /// <summary>
    /// 图鉴管理器（T21）。条目注册 + 解锁状态 + 品质升级 + 完成度统计（T23）。
    /// 解锁双触发：CraftFlowCompletedEvent（制作完成）与
    /// GiftPresentedEvent / BeatStartedEvent / DialogueNodeEnteredEvent（叙事推进）。
    /// 解锁状态经 PlayerPrefs 持久化（key: codex.unlocked.{entryId} → (int)quality+1）。
    /// </summary>
    public class CodexManager : MonoBehaviour
    {
        public static CodexManager Instance { get; private set; }

        [Tooltip("全部图鉴条目（Demo 17 条）")]
        [SerializeField] private CodexEntrySO[] _entries = new CodexEntrySO[0];

        private readonly Dictionary<string, CodexEntrySO> _entryById = new();
        private readonly Dictionary<string, CodexQuality> _unlocked = new();
        private IEventBus _eventBus;

        public IReadOnlyList<CodexEntrySO> Entries => _entries;
        public int TotalCount => _entries.Length;
        public int UnlockedCount => _unlocked.Count;

        /// <summary>完成度百分比（0-100，T23）。</summary>
        public float CompletionPercent => _entries.Length == 0
            ? 0f
            : _unlocked.Count * 100f / _entries.Length;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            _eventBus = EventBus.Default;

            foreach (var entry in _entries)
            {
                if (entry == null || string.IsNullOrEmpty(entry.entryId)) continue;
                _entryById[entry.entryId] = entry;
            }

            LoadProgress();
        }

        private void OnEnable()
        {
            if (_eventBus == null) _eventBus = EventBus.Default;
            _eventBus.Subscribe<CraftFlowCompletedEvent>(OnCraftCompleted);
            _eventBus.Subscribe<GiftPresentedEvent>(OnGiftPresented);
            _eventBus.Subscribe<BeatStartedEvent>(OnBeatStarted);
            _eventBus.Subscribe<DialogueNodeEnteredEvent>(OnDialogueNodeEntered);
        }

        private void OnDisable()
        {
            _eventBus?.Unsubscribe<CraftFlowCompletedEvent>(OnCraftCompleted);
            _eventBus?.Unsubscribe<GiftPresentedEvent>(OnGiftPresented);
            _eventBus?.Unsubscribe<BeatStartedEvent>(OnBeatStarted);
            _eventBus?.Unsubscribe<DialogueNodeEnteredEvent>(OnDialogueNodeEntered);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // ---------------- 查询 ----------------

        public bool IsUnlocked(string entryId)
        {
            return _unlocked.ContainsKey(entryId);
        }

        public CodexQuality GetQuality(string entryId)
        {
            return _unlocked.TryGetValue(entryId, out var q) ? q : CodexQuality.Bronze;
        }

        public bool TryGetEntry(string entryId, out CodexEntrySO entry)
        {
            return _entryById.TryGetValue(entryId, out entry);
        }

        // ---------------- 解锁 ----------------

        /// <summary>
        /// 解锁条目。已解锁时尝试品质升级（只升不降）。
        /// </summary>
        public void UnlockEntry(string entryId, CodexQuality quality = CodexQuality.Bronze)
        {
            if (!_entryById.TryGetValue(entryId, out var entry))
            {
                Debug.LogWarning($"[CodexManager] 未知条目: {entryId}");
                return;
            }

            if (_unlocked.TryGetValue(entryId, out var old))
            {
                if (quality <= old) return; // 只升不降
                _unlocked[entryId] = quality;
                SaveEntry(entryId, quality);
                PublishCompletion();
                _eventBus.Publish(new CodexQualityUpgradedEvent
                {
                    entryId = entryId,
                    oldQuality = old,
                    newQuality = quality
                });
                Debug.Log($"[CodexManager] 品质升级: {entry.displayName} {old} → {quality}");
                return;
            }

            _unlocked[entryId] = quality;
            SaveEntry(entryId, quality);
            PublishCompletion();
            _eventBus.Publish(new CodexEntryUnlockedEvent
            {
                entryId = entryId,
                category = entry.category,
                displayName = entry.displayName,
                quality = quality
            });
            Debug.Log($"[CodexManager] 解锁条目: [{CodexEntrySO.CategoryName(entry.category)}] {entry.displayName} ({quality})");
        }

        /// <summary>清空解锁进度（测试/重开档用）。</summary>
        public void ResetProgress()
        {
            foreach (var entry in _entries)
            {
                if (entry != null) PlayerPrefs.DeleteKey(PrefKey(entry.entryId));
            }
            PlayerPrefs.Save();
            _unlocked.Clear();

            // startUnlocked 条目重新解锁
            foreach (var entry in _entries)
            {
                if (entry != null && entry.startUnlocked) UnlockEntry(entry.entryId);
            }
            PublishCompletion();
        }

        // ---------------- 事件触发 ----------------

        private void OnCraftCompleted(CraftFlowCompletedEvent evt)
        {
            if (string.IsNullOrEmpty(evt.recipeId)) return;

            // 品质映射：整体品质 ≥0.95 金 / ≥0.8 银 / 其余铜
            var quality = evt.overallQuality >= 0.95f ? CodexQuality.Gold
                : evt.overallQuality >= 0.8f ? CodexQuality.Silver
                : CodexQuality.Bronze;

            UnlockBySource(CodexUnlockSource.Craft, evt.recipeId, quality);
        }

        private void OnGiftPresented(GiftPresentedEvent evt)
        {
            UnlockBySource(CodexUnlockSource.Gift, evt.pastryId, CodexQuality.Bronze);
        }

        private void OnBeatStarted(BeatStartedEvent evt)
        {
            UnlockBySource(CodexUnlockSource.Beat, evt.beatId, CodexQuality.Bronze);
        }

        private void OnDialogueNodeEntered(DialogueNodeEnteredEvent evt)
        {
            UnlockBySource(CodexUnlockSource.DialogueNode, evt.nodeId, CodexQuality.Bronze);
        }

        private void UnlockBySource(CodexUnlockSource source, string sourceId, CodexQuality quality)
        {
            if (string.IsNullOrEmpty(sourceId)) return;

            foreach (var entry in _entries)
            {
                if (entry == null || entry.unlockSource != source) continue;
                if (entry.sourceId != sourceId) continue;
                UnlockEntry(entry.entryId, quality);
            }
        }

        // ---------------- 完成度（T23）----------------

        private void PublishCompletion()
        {
            _eventBus.Publish(new CodexCompletionChangedEvent
            {
                unlockedCount = UnlockedCount,
                totalCount = TotalCount,
                percent = CompletionPercent
            });
        }

        // ---------------- 持久化 ----------------

        private void LoadProgress()
        {
            foreach (var entry in _entries)
            {
                if (entry == null || string.IsNullOrEmpty(entry.entryId)) continue;

                int saved = PlayerPrefs.GetInt(PrefKey(entry.entryId), 0);
                if (saved > 0)
                {
                    _unlocked[entry.entryId] = (CodexQuality)(saved - 1);
                }
                else if (entry.startUnlocked)
                {
                    UnlockEntry(entry.entryId);
                }
            }
            PublishCompletion();
        }

        private void SaveEntry(string entryId, CodexQuality quality)
        {
            PlayerPrefs.SetInt(PrefKey(entryId), (int)quality + 1);
            PlayerPrefs.Save();
        }

        private static string PrefKey(string entryId) => $"codex.unlocked.{entryId}";
    }
}
