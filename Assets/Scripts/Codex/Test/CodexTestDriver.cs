using UnityEngine;
using PastryWorld.Core;
using PastryWorld.Craft;

namespace PastryWorld.Codex
{
    /// <summary>
    /// T21/T22/T23 图鉴线测试驱动。
    /// O=开关图鉴 UI / U=解锁下一条 / C=模拟制作完成（配方 1，品质随机） /
    /// G=模拟奉上点心 / R=清档重算。完成后打印完成度。
    /// </summary>
    public class CodexTestDriver : MonoBehaviour
    {
        private CodexUI _ui;
        private CodexManager _manager;
        private int _unlockCursor;
        private int _craftRound;

        private void Start()
        {
            _ui = FindFirstObjectByType<CodexUI>();
            _manager = CodexManager.Instance;
            EventBus.Default.Subscribe<CodexCompletionChangedEvent>(OnCompletion);
            EventBus.Default.Subscribe<CodexEntryUnlockedEvent>(OnUnlocked);

            Debug.Log($"[CodexDriver] 就绪。O=开关图鉴 U=解锁下一条 C=模拟制作 G=模拟奉上 R=清档");
        }

        private void OnDestroy()
        {
            EventBus.Default.Unsubscribe<CodexCompletionChangedEvent>(OnCompletion);
            EventBus.Default.Unsubscribe<CodexEntryUnlockedEvent>(OnUnlocked);
        }

        private void Update()
        {
            if (UnityEngine.Input.GetKeyDown(KeyCode.O)) _ui.Toggle();
            else if (UnityEngine.Input.GetKeyDown(KeyCode.U)) UnlockNext();
            else if (UnityEngine.Input.GetKeyDown(KeyCode.C)) SimulateCraft();
            else if (UnityEngine.Input.GetKeyDown(KeyCode.G)) SimulateGift();
            else if (UnityEngine.Input.GetKeyDown(KeyCode.R)) ResetAll();
        }

        private void UnlockNext()
        {
            if (_manager == null) return;
            var entries = _manager.Entries;
            for (int i = 0; i < entries.Count; i++)
            {
                var entry = entries[(_unlockCursor + i) % entries.Count];
                if (entry == null || _manager.IsUnlocked(entry.entryId)) continue;
                _unlockCursor = (_unlockCursor + i + 1) % entries.Count;
                _manager.UnlockEntry(entry.entryId, CodexQuality.Silver);
                return;
            }
            Debug.Log("[CodexDriver] 全部条目均已解锁");
        }

        private void SimulateCraft()
        {
            _craftRound++;
            // 品质随轮次递增：铜 → 银 → 金（验证只升不降）
            float quality = _craftRound >= 3 ? 0.97f : _craftRound == 2 ? 0.85f : 0.6f;
            EventBus.Default.Publish(new CraftFlowCompletedEvent
            {
                recipeId = "recipe_honey_fruit",
                overallQuality = quality,
                allStepsSuccess = _craftRound >= 2,
                totalSteps = 3,
                successSteps = _craftRound >= 2 ? 3 : 2
            });
        }

        private void SimulateGift()
        {
            EventBus.Default.Publish(new Narrative.GiftPresentedEvent
            {
                dialogueId = "D1",
                nodeId = "n2",
                pastryId = "honey_fruit"
            });
        }

        private void ResetAll()
        {
            _unlockCursor = 0;
            _craftRound = 0;
            _manager.ResetProgress();
        }

        private void OnCompletion(CodexCompletionChangedEvent evt)
        {
            Debug.Log($"[CodexDriver] 完成度变化: {evt.unlockedCount}/{evt.totalCount} = {evt.percent:F1}%");
        }

        private void OnUnlocked(CodexEntryUnlockedEvent evt)
        {
            Debug.Log($"[CodexDriver] 收到解锁事件: {evt.displayName} ({evt.quality})");
        }
    }
}
