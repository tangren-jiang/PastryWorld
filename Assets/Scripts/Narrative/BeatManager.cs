using System.Collections.Generic;
using UnityEngine;
using PastryWorld.Core;

namespace PastryWorld.Narrative
{
    /// <summary>
    /// 节拍状态机。管理叙事节拍的线性锁链流转。
    /// 技术预判报告 T13。
    /// </summary>
    public class BeatManager : MonoBehaviour
    {
        [Header("节拍数据")]
        [SerializeField] private BeatSO[] _beats;
        [SerializeField] private string _startBeatId;

        private Dictionary<string, BeatSO> _beatLookup;
        private BeatSO _currentBeat;
        private IEventBus _eventBus;

        public BeatSO CurrentBeat => _currentBeat;
        public string CurrentBeatId => _currentBeat != null ? _currentBeat.beatId : null;
        public bool IsRunning { get; private set; }

        void Awake()
        {
            BuildLookup();
        }

        void Start()
        {
            // 尝试获取 EventBus（由 BootstrapLoader 注入或场景中查找）
            _eventBus = FindEventBus();
        }

        /// <summary>
        /// 启动节拍链。
        /// </summary>
        public void StartChain(string startBeatId = null)
        {
            string id = string.IsNullOrEmpty(startBeatId) ? _startBeatId : startBeatId;
            if (string.IsNullOrEmpty(id))
            {
                Debug.LogWarning("[BeatManager] 未指定起始节拍 ID");
                return;
            }

            if (!_beatLookup.TryGetValue(id, out var beat))
            {
                Debug.LogError($"[BeatManager] 找不到节拍: {id}");
                return;
            }

            IsRunning = true;
            EnterBeat(beat);
        }

        /// <summary>
        /// 前进到下一个节拍。
        /// </summary>
        public void Advance()
        {
            if (!IsRunning || _currentBeat == null)
            {
                Debug.LogWarning("[BeatManager] 节拍链未运行或当前无节拍");
                return;
            }

            // 发布当前节拍完成事件
            _eventBus?.Publish(new BeatCompletedEvent { beatId = _currentBeat.beatId });

            if (string.IsNullOrEmpty(_currentBeat.nextBeatId))
            {
                // 节拍链结束
                IsRunning = false;
                _eventBus?.Publish(new BeatChainCompletedEvent { lastBeatId = _currentBeat.beatId });
                Debug.Log($"[BeatManager] 节拍链结束: {_currentBeat.beatId}");
                _currentBeat = null;
                return;
            }

            if (!_beatLookup.TryGetValue(_currentBeat.nextBeatId, out var nextBeat))
            {
                Debug.LogError($"[BeatManager] 找不到下一节拍: {_currentBeat.nextBeatId}");
                IsRunning = false;
                return;
            }

            EnterBeat(nextBeat);
        }

        /// <summary>
        /// 跳转到指定节拍（用于分支选择）。
        /// </summary>
        public void JumpTo(string beatId)
        {
            if (!_beatLookup.TryGetValue(beatId, out var beat))
            {
                Debug.LogError($"[BeatManager] 跳转失败，找不到节拍: {beatId}");
                return;
            }

            if (_currentBeat != null)
                _eventBus?.Publish(new BeatCompletedEvent { beatId = _currentBeat.beatId });

            EnterBeat(beat);
        }

        private void EnterBeat(BeatSO beat)
        {
            _currentBeat = beat;
            Debug.Log($"[BeatManager] 进入节拍: {beat.beatId} ({beat.beatType})");

            _eventBus?.Publish(new BeatStartedEvent
            {
                beatId = beat.beatId,
                beatType = beat.beatType,
                dialogueText = beat.dialogueText
            });

            // 根据节拍类型处理特殊逻辑
            switch (beat.beatType)
            {
                case BeatType.Transition:
                    // 转场节拍：自动前进（实际转场由 T12 处理）
                    break;
                case BeatType.Action:
                    // 行动节拍：等待玩家完成制作/交互后调用 Advance()
                    break;
                case BeatType.Memory:
                    // 记忆节拍：播放记忆后自动前进（由 T15 处理）
                    break;
                case BeatType.Choice:
                    // 选择节拍：等待玩家选择后调用 JumpTo()
                    break;
                case BeatType.Dialogue:
                    // 对话节拍：显示对话文本，等待玩家点击继续
                    break;
            }
        }

        private void BuildLookup()
        {
            _beatLookup = new Dictionary<string, BeatSO>();
            if (_beats == null) return;

            foreach (var beat in _beats)
            {
                if (beat == null || string.IsNullOrEmpty(beat.beatId))
                {
                    Debug.LogWarning("[BeatManager] 发现空节拍或无 ID 的节拍");
                    continue;
                }

                if (_beatLookup.ContainsKey(beat.beatId))
                {
                    Debug.LogWarning($"[BeatManager] 重复的节拍 ID: {beat.beatId}");
                    continue;
                }

                _beatLookup[beat.beatId] = beat;
            }
        }

        private static IEventBus FindEventBus()
        {
            // 临时：创建独立实例。后续 W6 集成时由 BootstrapLoader 统一注入。
            return new EventBus();
        }
    }
}
