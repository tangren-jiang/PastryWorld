using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace PastryWorld.Reality
{
    /// <summary>
    /// 现实场景情感色调变体（T18）。Demo 办公室三变体：
    /// variant_0 = Neutral（中性）/ variant_1 = Cold（冷灰）/ variant_2 = Warm（暖橙）。
    /// </summary>
    public enum ToneVariant
    {
        Neutral = 0,
        Cold = 1,
        Warm = 2
    }

    /// <summary>
    /// 场景渐进色调系统（T18）。
    /// URP Volume（全局）+ ColorAdjustments 运行时插值：
    /// 单 Volume 单 Profile 直接 lerp 参数值（规避多 Profile 权重叠加问题）。
    /// 相机后处理在 EnsureVolume 时自动开启。
    /// 与 T12 水墨转场集成时序：在 InkTransitionController.Transition 的 onCovered
    /// 回调里调 TransitionTo（Profile 过渡与墨迹揭示重叠，总时长不过长）。
    /// </summary>
    public class SceneToneManager : MonoBehaviour
    {
        private static SceneToneManager _instance;

        /// <summary>全局实例（惰性创建，跨场景常驻）。</summary>
        public static SceneToneManager Instance
        {
            get
            {
                if (_instance != null) return _instance;
                var go = new GameObject("SceneToneManager");
                DontDestroyOnLoad(go);
                _instance = go.AddComponent<SceneToneManager>();
                return _instance;
            }
        }

        [Header("可选：天空着色（SpriteRenderer，按情感进度 Gradient 采样）")]
        [SerializeField] private SpriteRenderer _skyRenderer;

        [SerializeField] private Gradient _skyGradient = DefaultSkyGradient();

        [Header("过渡")]
        [Tooltip("默认过渡时长（秒）。报告建议 0.5-1.0s")]
        [SerializeField, Range(0.2f, 3f)] private float _defaultDuration = 1f;

        private Volume _volume;
        private ColorAdjustments _adjustments;
        private Coroutine _routine;
        private float _skyProgress = 0.5f;

        /// <summary>当前天空情感进度（0=冷灰现实，1=温暖）。</summary>
        public float SkyProgress => _skyProgress;

        void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            EnsureVolume();
        }

        void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }

        /// <summary>便捷静态入口：过渡到指定变体。</summary>
        public static void Transition(ToneVariant variant, float duration = -1f)
        {
            Instance.TransitionTo(variant, duration);
        }

        /// <summary>过渡到指定色调变体（插值 ColorAdjustments + 天空 Gradient）。</summary>
        public void TransitionTo(ToneVariant variant, float duration = -1f)
        {
            EnsureVolume();
            if (duration <= 0f) duration = _defaultDuration;

            var target = ParamsOf(variant);
            float targetSky = variant == ToneVariant.Cold ? 0f
                : variant == ToneVariant.Warm ? 1f : 0.5f;

            StartTransition(target, targetSky, duration);
        }

        /// <summary>
        /// 情感进度连续插值（T18 emotionalProgress 0.0-1.0）。
        /// 0 = Cold（冷灰现实），1 = Warm（温暖），中间值连续混合。
        /// </summary>
        public void SetEmotionalProgress(float progress, float duration = -1f)
        {
            EnsureVolume();
            if (duration <= 0f) duration = _defaultDuration;

            progress = Mathf.Clamp01(progress);
            var target = LerpParams(ColdParams, WarmParams, progress);
            StartTransition(target, progress, duration);
        }

        private void StartTransition(ToneParams target, float targetSky, float duration)
        {
            if (_routine != null) StopCoroutine(_routine);
            _routine = StartCoroutine(TransitionRoutine(target, targetSky, duration));
        }

        private IEnumerator TransitionRoutine(ToneParams target, float targetSky, float duration)
        {
            var start = CurrentParams();
            float startSky = _skyProgress;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
                ApplyParams(LerpParams(start, target, t));
                _skyProgress = Mathf.Lerp(startSky, targetSky, t);
                ApplySky();
                yield return null;
            }

            ApplyParams(target);
            _skyProgress = targetSky;
            ApplySky();
            _routine = null;
        }

        // ---- 参数 ----

        private readonly struct ToneParams
        {
            public readonly float Exposure;
            public readonly float Saturation;
            public readonly Color Filter;

            public ToneParams(float exposure, float saturation, Color filter)
            {
                Exposure = exposure;
                Saturation = saturation;
                Filter = filter;
            }
        }

        private static ToneParams NeutralParams => new ToneParams(0f, 0f, Color.white);
        private static ToneParams ColdParams => new ToneParams(-0.35f, -35f, new Color(0.72f, 0.76f, 0.86f));
        private static ToneParams WarmParams => new ToneParams(0.25f, 12f, new Color(1f, 0.88f, 0.74f));

        private static ToneParams ParamsOf(ToneVariant variant)
        {
            switch (variant)
            {
                case ToneVariant.Cold: return ColdParams;
                case ToneVariant.Warm: return WarmParams;
                default: return NeutralParams;
            }
        }

        private static ToneParams LerpParams(ToneParams a, ToneParams b, float t)
        {
            return new ToneParams(
                Mathf.Lerp(a.Exposure, b.Exposure, t),
                Mathf.Lerp(a.Saturation, b.Saturation, t),
                Color.Lerp(a.Filter, b.Filter, t));
        }

        private ToneParams CurrentParams()
        {
            EnsureVolume();
            return new ToneParams(
                _adjustments.postExposure.value,
                _adjustments.saturation.value,
                _adjustments.colorFilter.value);
        }

        private void ApplyParams(ToneParams p)
        {
            _adjustments.postExposure.value = p.Exposure;
            _adjustments.saturation.value = p.Saturation;
            _adjustments.colorFilter.value = p.Filter;
        }

        private void ApplySky()
        {
            if (_skyRenderer != null)
                _skyRenderer.color = _skyGradient.Evaluate(_skyProgress);
        }

        // ---- Volume 构建（程序化，无需美术资产）----

        private void EnsureVolume()
        {
            if (_volume != null) return;

            var volumeGo = new GameObject("GlobalVolume");
            volumeGo.transform.SetParent(transform, false);

            _volume = volumeGo.AddComponent<Volume>();
            _volume.isGlobal = true;
            _volume.priority = 5;
            _volume.weight = 1f;

            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            profile.name = "RuntimeToneProfile";
            _adjustments = profile.Add<ColorAdjustments>(true);
            _adjustments.postExposure.overrideState = true;
            _adjustments.saturation.overrideState = true;
            _adjustments.colorFilter.overrideState = true;
            ApplyParams(NeutralParams);

            _volume.sharedProfile = profile;

            // 开启相机后处理（URP 默认关闭）
            var cam = Camera.main;
            if (cam != null)
                cam.GetUniversalAdditionalCameraData().renderPostProcessing = true;
        }

        private static Gradient DefaultSkyGradient()
        {
            return new Gradient
            {
                colorKeys = new[]
                {
                    new GradientColorKey(new Color(0.62f, 0.65f, 0.72f), 0f), // 冷灰现实
                    new GradientColorKey(Color.white, 0.5f),
                    new GradientColorKey(new Color(1f, 0.85f, 0.65f), 1f),   // 暖橙梦境
                }
            };
        }
    }
}
