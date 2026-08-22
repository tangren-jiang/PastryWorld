using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using PastryWorld.Core;

namespace PastryWorld.Narrative
{
    /// <summary>
    /// 记忆片段播放器框架。T6/T15 共享模块。
    /// 输入 MemoryClipData（帧序列+时长+音频+播放模式），输出播放控制。
    ///
    /// Overlay 模式（T6）：CanvasGroup.alpha=0.7，blocksRaycast=false（不拦截输入，
    /// 制作操作继续），层级在制作界面之上、暂停菜单之下。播完后淡出不中断制作。
    /// Fullscreen 模式（T15）：alpha=1，blocksRaycast=true（锁输入），本框架预留。
    /// 技术预判报告 T6/T15。
    /// </summary>
    public class MemoryClipPlayer : MonoBehaviour
    {
        [Header("层级（低于暂停菜单）")]
        [Tooltip("叠加模式 Canvas sortingOrder。制作界面之上、暂停菜单之下")]
        [SerializeField] private int _overlaySortingOrder = 500;

        [Tooltip("全屏模式 Canvas sortingOrder（T15）")]
        [SerializeField] private int _fullscreenSortingOrder = 900;

        [Header("可选：外部预建 Canvas（为空则运行时自建）")]
        [SerializeField] private Canvas _targetCanvas;

        private CanvasGroup _canvasGroup;
        private Image _frameImage;
        private AudioSource _audioSource;
        private IEventBus _eventBus;
        private Coroutine _playRoutine;
        private MemoryClipData _currentClip;

        /// <summary>当前正在播放的记忆片段。未播放时为 null。</summary>
        public MemoryClipData CurrentClip => _currentClip;

        /// <summary>是否正在播放（含淡入淡出阶段）。</summary>
        public bool IsPlaying => _playRoutine != null;

        void Awake()
        {
            _eventBus = EventBus.Default;
            EnsureAudioSource();
        }

        void OnDestroy()
        {
            if (_playRoutine != null)
            {
                StopCoroutine(_playRoutine);
                _playRoutine = null;
            }
        }

        /// <summary>
        /// 播放一段记忆。若正在播放则先停止当前片段。
        /// </summary>
        public void Play(MemoryClipData clip)
        {
            if (clip == null || clip.frames == null || clip.frames.Length == 0)
            {
                Debug.LogWarning("[MemoryClipPlayer] 无效的记忆片段（无帧数据）");
                return;
            }

            Stop();
            _playRoutine = StartCoroutine(PlayRoutine(clip));
        }

        /// <summary>
        /// 立即停止播放并隐藏（不发布完成事件）。
        /// </summary>
        public void Stop()
        {
            if (_playRoutine != null)
            {
                StopCoroutine(_playRoutine);
                _playRoutine = null;
            }

            if (_currentClip != null)
            {
                _currentClip = null;
            }

            if (_audioSource != null && _audioSource.isPlaying)
            {
                _audioSource.Stop();
            }

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0f;
            }
        }

        private IEnumerator PlayRoutine(MemoryClipData clip)
        {
            _currentClip = clip;
            BuildLayers(clip.playMode);

            float targetAlpha = clip.playMode == MemoryPlayMode.Overlay ? clip.overlayAlpha : 1f;
            _canvasGroup.alpha = 0f;
            _frameImage.sprite = clip.frames[0];

            _eventBus?.Publish(new MemoryClipStartedEvent
            {
                clipId = clip.clipId,
                playMode = clip.playMode
            });

            if (clip.audioClip != null)
            {
                _audioSource.PlayOneShot(clip.audioClip);
            }

            // 淡入
            if (clip.fadeInDuration > 0f)
            {
                yield return FadeRoutine(0f, targetAlpha, clip.fadeInDuration);
            }
            else
            {
                _canvasGroup.alpha = targetAlpha;
            }

            // 帧序列播放（累积时间驱动，帧率波动安全；最后一帧保持到结束）
            float interval = 1f / Mathf.Max(1f, clip.fps);
            int frameIndex = 0;
            float elapsed = 0f;
            float playDuration = clip.Duration;

            while (elapsed < playDuration)
            {
                elapsed += Time.deltaTime;
                int next = Mathf.Min(Mathf.FloorToInt(elapsed / interval), clip.frames.Length - 1);
                if (next != frameIndex)
                {
                    frameIndex = next;
                    _frameImage.sprite = clip.frames[frameIndex];
                }
                yield return null;
            }

            // 淡出（规范：0.5s，不中断制作）
            if (clip.fadeOutDuration > 0f)
            {
                yield return FadeRoutine(targetAlpha, 0f, clip.fadeOutDuration);
            }

            _canvasGroup.alpha = 0f;

            _eventBus?.Publish(new MemoryClipCompletedEvent
            {
                clipId = clip.clipId,
                playMode = clip.playMode
            });

            _currentClip = null;
            _playRoutine = null;
        }

        private IEnumerator FadeRoutine(float from, float to, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                _canvasGroup.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / duration));
                yield return null;
            }
            _canvasGroup.alpha = to;
        }

        /// <summary>
        /// 构建播放层。Canvas + CanvasGroup + Image。
        /// Overlay：blocksRaycast=false（关键：不拦截输入）；Fullscreen：blocksRaycast=true。
        /// </summary>
        private void BuildLayers(MemoryPlayMode mode)
        {
            if (_targetCanvas == null)
            {
                var go = new GameObject("MemoryClipPlayer_Canvas");
                go.transform.SetParent(transform, false);
                _targetCanvas = go.AddComponent<Canvas>();
                _targetCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            }

            _targetCanvas.sortingOrder = mode == MemoryPlayMode.Overlay
                ? _overlaySortingOrder
                : _fullscreenSortingOrder;

            _canvasGroup = _targetCanvas.GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
            {
                _canvasGroup = _targetCanvas.gameObject.AddComponent<CanvasGroup>();
            }

            // 关键：叠加模式不拦截射线，制作输入继续
            _canvasGroup.blocksRaycasts = mode == MemoryPlayMode.Fullscreen;
            _canvasGroup.interactable = mode == MemoryPlayMode.Fullscreen;
            _canvasGroup.alpha = 0f;

            if (_frameImage == null)
            {
                var imageGo = new GameObject("Frame");
                imageGo.transform.SetParent(_targetCanvas.transform, false);
                _frameImage = imageGo.AddComponent<Image>();
                var rt = imageGo.GetComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
            }

            _frameImage.preserveAspect = true;
            _frameImage.raycastTarget = false;
        }

        private void EnsureAudioSource()
        {
            _audioSource = GetComponent<AudioSource>();
            if (_audioSource == null)
            {
                _audioSource = gameObject.AddComponent<AudioSource>();
                _audioSource.playOnAwake = false;
            }
        }
    }
}
