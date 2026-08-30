using System;
using System.Collections.Generic;
using UnityEngine;
using PastryWorld.Core;
using PastryWorld.Exploration;

namespace PastryWorld.Narrative
{
    /// <summary>
    /// 对话引擎核心（T14）。
    /// 职责：对话流转（节点推进/分支/奉上/结束）+ EventBus 事件发布
    /// + T13 节拍联动（BeatStartedEvent → 对话 → Advance()）+ 对话期间锁定玩家输入。
    /// </summary>
    public class DialogueManager : MonoBehaviour
    {
        /// <summary>节拍-对话绑定（T13 联动：进入 Dialogue 节拍自动播绑定的对话）。</summary>
        [Serializable]
        public class BeatDialogueBinding
        {
            public string beatId;
            public DialogueSO dialogue;
        }

        [Header("节拍联动（可选）")]
        [SerializeField] private List<BeatDialogueBinding> _beatBindings = new List<BeatDialogueBinding>();

        [Header("UI（为空则运行时自建）")]
        [SerializeField] private DialogueUI _ui;

        private static DialogueManager _instance;

        /// <summary>全局实例（惰性创建）。</summary>
        public static DialogueManager Instance
        {
            get
            {
                if (_instance != null) return _instance;
                var go = new GameObject("DialogueManager");
                _instance = go.AddComponent<DialogueManager>();
                return _instance;
            }
        }

        /// <summary>是否有对话正在进行。</summary>
        public bool IsDialogueActive { get; private set; }

        public DialogueSO CurrentDialogue { get; private set; }

        private DialogueNode _currentNode;
        private BeatManager _beatManager;
        private IEventBus _eventBus;
        private bool _startedFromBeat;
        private PlayerController _lockedPlayer;

        void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            _eventBus = EventBus.Default;

            if (_ui == null)
            {
                var uiGo = new GameObject("DialogueUI");
                uiGo.transform.SetParent(transform, false);
                _ui = uiGo.AddComponent<DialogueUI>();
            }

            _ui.AdvanceClicked += HandleAdvance;
            _ui.ChoiceClicked += SelectChoice;
            _ui.GiftClicked += PresentGift;
        }

        void OnEnable()
        {
            _eventBus.Subscribe<BeatStartedEvent>(OnBeatStarted);
        }

        void OnDisable()
        {
            _eventBus.Unsubscribe<BeatStartedEvent>(OnBeatStarted);
        }

        void OnDestroy()
        {
            if (_ui != null)
            {
                _ui.AdvanceClicked -= HandleAdvance;
                _ui.ChoiceClicked -= SelectChoice;
                _ui.GiftClicked -= PresentGift;
            }

            SetPlayerLocked(false);

            if (_instance == this)
                _instance = null;
        }

        /// <summary>开始一段对话。已有对话进行中则忽略。返回是否真正开始。</summary>
        public bool StartDialogue(DialogueSO dialogue)
        {
            if (dialogue == null) return false;
            if (IsDialogueActive)
            {
                Debug.LogWarning($"[DialogueManager] 对话 {CurrentDialogue?.dialogueId} 进行中，忽略新对话 {dialogue.dialogueId}");
                return false;
            }

            var startNode = dialogue.StartNode;
            if (startNode == null)
            {
                Debug.LogError($"[DialogueManager] 对话 {dialogue.dialogueId} 无入口节点（startNodeId={dialogue.startNodeId}）");
                return false;
            }

            CurrentDialogue = dialogue;
            IsDialogueActive = true;

            SetPlayerLocked(true);
            _eventBus.Publish(new DialogueStartedEvent { dialogueId = dialogue.dialogueId });
            EnterNode(startNode);
            return true;
        }

        /// <summary>无分支节点的推进（UI 点击）。</summary>
        public void HandleAdvance()
        {
            if (!IsDialogueActive || _currentNode == null) return;

            if (_currentNode.choices.Count > 0) return; // 分支节点走 SelectChoice

            GoToNext(_currentNode.nextNodeId);
        }

        /// <summary>玩家选择分支。</summary>
        public void SelectChoice(int index)
        {
            if (!IsDialogueActive || _currentNode == null) return;
            if (index < 0 || index >= _currentNode.choices.Count) return;

            var choice = _currentNode.choices[index];
            _eventBus.Publish(new DialogueChoiceMadeEvent
            {
                dialogueId = CurrentDialogue.dialogueId,
                nodeId = _currentNode.nodeId,
                choiceIndex = index,
                choiceText = choice.text
            });

            GoToNext(choice.nextNodeId);
        }

        /// <summary>
        /// 奉上点心（T14 核心机制）：消耗背包中的点心并跳转 GiftResponseNode。
        /// 按钮只在该节点 giftPastryId 有货时出现，此处再做一次防御检查。
        /// </summary>
        public void PresentGift()
        {
            if (!IsDialogueActive || _currentNode == null) return;
            if (string.IsNullOrEmpty(_currentNode.giftPastryId)) return;
            if (!SimpleInventory.Has(_currentNode.giftPastryId)) return;

            var item = SimpleInventory.Take(_currentNode.giftPastryId);
            _eventBus.Publish(new GiftPresentedEvent
            {
                dialogueId = CurrentDialogue.dialogueId,
                nodeId = _currentNode.nodeId,
                pastryId = _currentNode.giftPastryId
            });

            Debug.Log($"[DialogueManager] 奉上点心：{item?.displayName ?? _currentNode.giftPastryId}");
            GoToNext(_currentNode.giftResponseNodeId);
        }

        private void EnterNode(DialogueNode node)
        {
            _currentNode = node;

            _eventBus.Publish(new DialogueNodeEnteredEvent
            {
                dialogueId = CurrentDialogue.dialogueId,
                nodeId = node.nodeId,
                speakerName = node.speakerName
            });

            // 环境联动（T14 envTrigger → EventBus → 消费方响应）
            if (!string.IsNullOrEmpty(node.envTrigger))
                _eventBus.Publish(new EnvironmentChangedEvent { triggerId = node.envTrigger });

            _ui.ShowNode(node);
        }

        private void GoToNext(string nextNodeId)
        {
            if (string.IsNullOrEmpty(nextNodeId))
            {
                EndDialogue();
                return;
            }

            var next = CurrentDialogue.GetNode(nextNodeId);
            if (next == null)
            {
                Debug.LogError($"[DialogueManager] 对话 {CurrentDialogue.dialogueId} 缺少节点 {nextNodeId}，对话终止");
                EndDialogue();
                return;
            }

            EnterNode(next);
        }

        private void EndDialogue()
        {
            string dialogueId = CurrentDialogue != null ? CurrentDialogue.dialogueId : null;

            _ui.Hide();
            _currentNode = null;
            CurrentDialogue = null;
            IsDialogueActive = false;

            SetPlayerLocked(false);
            _eventBus.Publish(new DialogueEndedEvent { dialogueId = dialogueId });

            // T13 节拍联动：对话结束 → 节拍推进（可能触发下一个对话节拍）
            if (_startedFromBeat)
            {
                _startedFromBeat = false;
                if (_beatManager == null) _beatManager = FindFirstObjectByType<BeatManager>();
                _beatManager?.Advance();
            }
        }

        private void OnBeatStarted(BeatStartedEvent evt)
        {
            var binding = _beatBindings.Find(b => b.beatId == evt.beatId);
            if (binding == null || binding.dialogue == null) return;

            // 仅当对话真正开始时才标记节拍联动，拒绝路径不泄漏标志
            _startedFromBeat = StartDialogue(binding.dialogue);
        }

        private void SetPlayerLocked(bool locked)
        {
            if (_lockedPlayer == null && locked)
            {
                _lockedPlayer = FindFirstObjectByType<PlayerController>();
            }

            if (_lockedPlayer != null)
            {
                _lockedPlayer.InputLocked = locked;
                if (!locked) _lockedPlayer = null;
            }
        }
    }
}
