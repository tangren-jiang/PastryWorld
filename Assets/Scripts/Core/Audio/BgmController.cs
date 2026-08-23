using System.Collections;
using UnityEngine;

namespace PastryWorld.Core
{
    /// <summary>
    /// BGM 控制器（T15 BGM 切换）。双 AudioSource 交叉淡入淡出。
    /// 惰性单例 + DontDestroyOnLoad，跨场景常驻。
    /// </summary>
    public class BgmController : MonoBehaviour
    {
        private static BgmController _instance;

        /// <summary>全局实例（惰性创建）。</summary>
        public static BgmController Instance
        {
            get
            {
                if (_instance != null) return _instance;

                var go = new GameObject("BgmController");
                DontDestroyOnLoad(go);
                _instance = go.AddComponent<BgmController>();
                return _instance;
            }
        }

        [Tooltip("默认交叉淡入淡出时长（秒）")]
        [SerializeField, Range(0f, 3f)] private float _defaultFade = 0.8f;

        private AudioSource _sourceA;
        private AudioSource _sourceB;
        private AudioSource _active;
        private Coroutine _fadeRoutine;

        /// <summary>当前播放的 BGM 片段（无播放时为 null）。</summary>
        public AudioClip CurrentClip => _active != null && _active.isPlaying ? _active.clip : null;

        void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;

            _sourceA = CreateSource("BgmSource_A");
            _sourceB = CreateSource("BgmSource_B");
            _active = _sourceA;
        }

        void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }

        /// <summary>
        /// 切换 BGM（交叉淡入淡出）。同一片段已在播放时不做任何事。
        /// </summary>
        public void Play(AudioClip clip, float fade = -1f)
        {
            if (clip == null) return;
            if (fade < 0f) fade = _defaultFade;

            // 非活跃音源上恰好是同一片段（刚切走又切回）：直接交叉回来
            if (_active.isPlaying && _active.clip == clip) return;

            var target = _active == _sourceA ? _sourceB : _sourceA;
            target.clip = clip;
            target.volume = 0f;
            target.Play();

            if (_fadeRoutine != null) StopCoroutine(_fadeRoutine);
            _fadeRoutine = StartCoroutine(CrossFadeRoutine(target, _active, fade));
            _active = target;
        }

        /// <summary>停止 BGM（淡出）。</summary>
        public void Stop(float fade = -1f)
        {
            if (fade < 0f) fade = _defaultFade;

            if (!_active.isPlaying) return;

            if (_fadeRoutine != null) StopCoroutine(_fadeRoutine);
            _fadeRoutine = StartCoroutine(FadeOutRoutine(_active, fade));
        }

        private IEnumerator CrossFadeRoutine(AudioSource fadeIn, AudioSource fadeOut, float duration)
        {
            float startIn = fadeIn.volume;
            float startOut = fadeOut.volume;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                fadeIn.volume = Mathf.Lerp(startIn, 1f, t);
                fadeOut.volume = Mathf.Lerp(startOut, 0f, t);
                yield return null;
            }

            fadeIn.volume = 1f;
            fadeOut.volume = 0f;
            fadeOut.Stop();
            _fadeRoutine = null;
        }

        private IEnumerator FadeOutRoutine(AudioSource source, float duration)
        {
            float start = source.volume;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                source.volume = Mathf.Lerp(start, 0f, Mathf.Clamp01(elapsed / duration));
                yield return null;
            }

            source.volume = 0f;
            source.Stop();
            _fadeRoutine = null;
        }

        private AudioSource CreateSource(string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var source = go.AddComponent<AudioSource>();
            source.loop = true;
            source.playOnAwake = false;
            source.volume = 0f;
            return source;
        }
    }
}
