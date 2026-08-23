using System.Collections.Generic;
using UnityEngine;
using PastryWorld.Core;
using PastryWorld.Exploration;

namespace PastryWorld.Reality
{
    /// <summary>
    /// 现实独白播放器（T17）。单例。
    /// 职责：整段独白流转（逐行推进/结束）+ EventBus 事件 + 播放期间锁定玩家输入。
    /// 模式复用 DialogueManager（T14）。
    /// </summary>
    public class RealityMonologueManager : MonoBehaviour
    {
        private static RealityMonologueManager _instance;

        /// <summary>全局实例（惰性创建）。</summary>
        public static RealityMonologueManager Instance
        {
            get
            {
                if (_instance != null) return _instance;
                var go = new GameObject("RealityMonologueManager");
                _instance = go.AddComponent<RealityMonologueManager>();
                return _instance;
            }
        }

        [Header("UI（为空则运行时自建）")]
        [SerializeField] private MonologueUI _ui;

        /// <summary>是否有独白正在进行。</summary>
        public bool IsMonologueActive { get; private set; }

        public RealityMonologueSO CurrentMonologue { get; private set; }

        private int _lineIndex;
        private IEventBus _eventBus;
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
                var uiGo = new GameObject("MonologueUI");
                uiGo.transform.SetParent(transform, false);
                _ui = uiGo.AddComponent<MonologueUI>();
            }

            _ui.AdvanceClicked += HandleAdvance;
        }

        void OnDestroy()
        {
            if (_ui != null)
                _ui.AdvanceClicked -= HandleAdvance;

            SetPlayerLocked(false);

            if (_instance == this)
                _instance = null;
        }

        /// <summary>开始一段独白。已有独白/对话进行中则忽略。</summary>
        public void Play(RealityMonologueSO monologue)
        {
            if (monologue == null) return;
            if (monologue.lines == null || monologue.lines.Count == 0)
            {
                Debug.LogWarning($"[RealityMonologueManager] 独白 {monologue.monologueId} 无内容");
                return;
            }
            if (IsMonologueActive) return;
            if (Narrative.DialogueManager.Instance.IsDialogueActive) return;

            CurrentMonologue = monologue;
            IsMonologueActive = true;
            _lineIndex = 0;

            SetPlayerLocked(true);
            _eventBus.Publish(new RealityMonologueStartedEvent { monologueId = monologue.monologueId });
            ShowCurrentLine();
        }

        /// <summary>点击推进：下一行，末行则结束。</summary>
        public void HandleAdvance()
        {
            if (!IsMonologueActive || CurrentMonologue == null) return;

            _lineIndex++;
            if (_lineIndex >= CurrentMonologue.lines.Count)
            {
                EndMonologue();
                return;
            }
            ShowCurrentLine();
        }

        /// <summary>外部强制中止（如场景切换）。</summary>
        public void Stop()
        {
            if (IsMonologueActive)
                EndMonologue();
        }

        private void ShowCurrentLine()
        {
            _ui.ShowLine(CurrentMonologue.lines[_lineIndex]);
        }

        private void EndMonologue()
        {
            string monologueId = CurrentMonologue != null ? CurrentMonologue.monologueId : null;

            _ui.Hide();
            CurrentMonologue = null;
            IsMonologueActive = false;

            SetPlayerLocked(false);
            _eventBus.Publish(new RealityMonologueEndedEvent { monologueId = monologueId });
        }

        private void SetPlayerLocked(bool locked)
        {
            if (_lockedPlayer == null && locked)
                _lockedPlayer = FindFirstObjectByType<PlayerController>();

            if (_lockedPlayer != null)
            {
                _lockedPlayer.InputLocked = locked;
                if (!locked) _lockedPlayer = null;
            }
        }
    }
}
