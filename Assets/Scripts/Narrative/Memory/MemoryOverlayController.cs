using System.Collections.Generic;
using UnityEngine;
using PastryWorld.Core;

namespace PastryWorld.Narrative
{
    /// <summary>
    /// 记忆叠加触发控制器。监听 BeatManager 的节拍事件，
    /// 当进入 Memory 类型节拍时自动播放对应的 MemoryClipData。
    /// 技术预判报告 T6。
    ///
    /// 播放映射：beatId → MemoryClipData（通过 _clips 数组或 clipId 匹配）。
    /// 叠加播放不中断制作（blocksRaycasts=false），
    /// 播完后是否自动推进节拍由 autoAdvanceAfterOverlay 决定。
    /// </summary>
    public class MemoryOverlayController : MonoBehaviour
    {
        [Header("引用")]
        [SerializeField] private MemoryClipPlayer _player;
        [SerializeField] private BeatManager _beatManager;

        [Header("记忆片段库（clipId 与 beatId 对应）")]
        [SerializeField] private MemoryClipData[] _clips;

        [Header("行为")]
        [Tooltip("叠加片段播完后是否自动推进节拍链。叠加片段不锁输入，通常由玩家完成制作后推进；全屏记忆（T15）必须推进")]
        [SerializeField] private bool _autoAdvanceAfterFullscreen = true;

        private readonly Dictionary<string, MemoryClipData> _clipLookup = new();
        private IEventBus _eventBus;

        void Awake()
        {
            BuildLookup();

            if (_player == null)
            {
                _player = GetComponent<MemoryClipPlayer>();
            }
        }

        void Start()
        {
            _eventBus = EventBus.Default;
            _eventBus.Subscribe<BeatStartedEvent>(OnBeatStarted);
            _eventBus.Subscribe<MemoryClipCompletedEvent>(OnMemoryClipCompleted);
        }

        void OnDestroy()
        {
            if (_eventBus != null)
            {
                _eventBus.Unsubscribe<BeatStartedEvent>(OnBeatStarted);
                _eventBus.Unsubscribe<MemoryClipCompletedEvent>(OnMemoryClipCompleted);
            }
        }

        private void OnBeatStarted(BeatStartedEvent evt)
        {
            if (evt.beatType != BeatType.Memory) return;

            if (_player == null)
            {
                Debug.LogWarning("[MemoryOverlay] 未配置 MemoryClipPlayer");
                return;
            }

            var clip = FindClip(evt.beatId);
            if (clip == null)
            {
                Debug.LogWarning($"[MemoryOverlay] 找不到记忆片段: {evt.beatId}，节拍将不播放记忆");
                return;
            }

            _player.Play(clip);
        }

        private void OnMemoryClipCompleted(MemoryClipCompletedEvent evt)
        {
            // 全屏记忆播完后推进节拍链（T15 管线，框架阶段先打通）
            if (_autoAdvanceAfterFullscreen && evt.playMode == MemoryPlayMode.Fullscreen)
            {
                _beatManager?.Advance();
            }
        }

        private MemoryClipData FindClip(string beatId)
        {
            if (_clipLookup.TryGetValue(beatId, out var clip))
            {
                return clip;
            }

            // 兜底：clipId 未填时按数组顺序找第一个未匹配的（容错，便于占位测试）
            foreach (var c in _clips)
            {
                if (c != null && string.IsNullOrEmpty(c.clipId))
                {
                    return c;
                }
            }

            return null;
        }

        private void BuildLookup()
        {
            _clipLookup.Clear();
            if (_clips == null) return;

            foreach (var clip in _clips)
            {
                if (clip == null || string.IsNullOrEmpty(clip.clipId))
                {
                    continue;
                }

                if (_clipLookup.ContainsKey(clip.clipId))
                {
                    Debug.LogWarning($"[MemoryOverlay] 重复的记忆片段 ID: {clip.clipId}");
                    continue;
                }

                _clipLookup[clip.clipId] = clip;
            }
        }
    }
}
