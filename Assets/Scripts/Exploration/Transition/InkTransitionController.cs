using System.Collections;
using PastryWorld.Core;
using UnityEngine;
using UnityEngine.UI;

namespace PastryWorld.Exploration
{
    /// <summary>
    /// 水墨转场控制器（T12）。
    /// 墨迹从屏幕中心扩散覆盖 → 执行场景切换等动作 → 墨迹收缩揭示。
    /// 运行时自建全屏 Canvas（sortingOrder=800，在制作界面之上、暂停菜单之下）。
    ///
    /// 用法：
    ///   InkTransitionController.Transition(() => { /* 场景切换 */ });
    /// </summary>
    public class InkTransitionController : MonoBehaviour
    {
        [Header("Shader（必须赋值以保证打进构建）")]
        [SerializeField] private Shader _inkShader;

        [Header("参数")]
        [SerializeField] private float _coverDuration = 0.5f;
        [SerializeField] private float _revealDuration = 0.6f;
        [SerializeField] private int _sortingOrder = 800;

        [Header("噪声纹理（为空时运行时程序化生成 Perlin）")]
        [SerializeField] private Texture2D _noiseTexture;

        private const int NoiseSize = 256;

        private static InkTransitionController _instance;
        private Material _inkMaterial;
        private Canvas _canvas;
        private Image _overlayImage;
        private Coroutine _running;
        private IEventBus _eventBus;

        /// <summary>全局实例（惰性创建，跨场景常驻）。</summary>
        public static InkTransitionController Instance
        {
            get
            {
                if (_instance != null) return _instance;
                var go = new GameObject("InkTransitionController");
                DontDestroyOnLoad(go);
                _instance = go.AddComponent<InkTransitionController>();
                _instance.EnsureSetup();
                return _instance;
            }
        }
        /// <summary>
        /// 便捷入口：墨迹覆盖 → onCovered 回调（场景切换等）→ 墨迹揭示。
        /// </summary>
        /// <param name="onCovered">屏幕完全被墨覆盖时执行的动作</param>
        /// <param name="coverDuration">覆盖时长（秒）</param>
        /// <param name="revealDuration">揭示时长（秒）</param>
        public static void Transition(System.Action onCovered,
            float coverDuration = -1f, float revealDuration = -1f)
        {
            var inst = Instance;
            float cover = coverDuration > 0f ? coverDuration : inst._coverDuration;
            float reveal = revealDuration > 0f ? revealDuration : inst._revealDuration;
            inst.Run(onCovered, cover, reveal);
        }

        void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            _eventBus = EventBus.Default;
        }

        void Start()
        {
            // 延迟到 Start：场景实例的 _inkShader SerializeField 此时已完成反序列化
            EnsureSetup();
        }

        void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }

        private void EnsureSetup()
        {
            if (_canvas != null) return;

            Shader shader = _inkShader != null ? _inkShader : Shader.Find("PastryWorld/InkTransition");
            if (shader == null)
            {
                Debug.LogError("[InkTransition] 未找到 PastryWorld/InkTransition shader", this);
                return;
            }

            if (_noiseTexture == null) _noiseTexture = GenerateNoiseTexture();
            if (_noiseTexture == null)
            {
                Debug.LogError("[InkTransition] 噪声纹理生成失败", this);
                return;
            }

            _inkMaterial = new Material(shader);
            _inkMaterial.SetTexture("_NoiseTex", _noiseTexture);
            _inkMaterial.SetFloat("_Progress", 0f);

            // 全屏 Canvas + Image（复用 MemoryClipPlayer 的运行时建 Canvas 模式）
            var go = new GameObject("InkTransition_Canvas");
            go.transform.SetParent(transform, false);
            _canvas = go.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = _sortingOrder;

            var imageGo = new GameObject("InkOverlay");
            imageGo.transform.SetParent(go.transform, false);
            _overlayImage = imageGo.AddComponent<Image>();
            _overlayImage.raycastTarget = true; // 转场期间拦截输入
            _overlayImage.material = _inkMaterial;
            _overlayImage.color = Color.white;

            // 全屏拉伸
            var rt = _overlayImage.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            SetVisible(false);
        }

        private void Run(System.Action onCovered, float cover, float reveal)
        {
            if (_inkMaterial == null) EnsureSetup();
            if (_inkMaterial == null)
            {
                Debug.LogWarning("[InkTransition] 材质未就绪（shader 缺失），跳过转场直接执行动作", this);
                onCovered?.Invoke();
                return;
            }
            if (_running != null) StopCoroutine(_running);
            _running = StartCoroutine(TransitionRoutine(onCovered, cover, reveal));
        }

        private IEnumerator TransitionRoutine(System.Action onCovered,
            float coverDuration, float revealDuration)
        {
            _eventBus.Publish(new InkTransitionStartedEvent { cover = true, duration = coverDuration });
            SetVisible(true);

            // 覆盖：progress 0 → 1，墨迹从中心扩散遮蔽屏幕
            yield return FadeRoutine(0f, 1f, coverDuration);

            onCovered?.Invoke();

            // 揭示：progress 1 → 0，墨迹收缩
            _eventBus.Publish(new InkTransitionStartedEvent { cover = false, duration = revealDuration });
            yield return FadeRoutine(1f, 0f, revealDuration);

            SetVisible(false);
            _running = null;
            _eventBus.Publish(new InkTransitionCompletedEvent
            {
                totalDuration = coverDuration + revealDuration
            });
        }

        private IEnumerator FadeRoutine(float from, float to, float duration)
        {
            float elapsed = 0f;
            if (duration <= 0f)
            {
                _inkMaterial.SetFloat("_Progress", to);
                yield break;
            }
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime; // 转场不受 timeScale 影响
                float t = Mathf.Clamp01(elapsed / duration);
                // EaseInOutQuad
                float eased = t < 0.5f ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 2f) / 2f;
                _inkMaterial.SetFloat("_Progress", Mathf.Lerp(from, to, eased));
                yield return null;
            }
            _inkMaterial.SetFloat("_Progress", to);
        }

        private void SetVisible(bool visible)
        {
            if (_canvas != null) _canvas.enabled = visible;
            if (!visible && _inkMaterial != null) _inkMaterial.SetFloat("_Progress", 0f);
        }

        /// <summary>程序化 Perlin 噪声纹理（wrap repeat，供墨迹边缘扰动）。</summary>
        private Texture2D GenerateNoiseTexture()
        {
            var tex = new Texture2D(NoiseSize, NoiseSize, TextureFormat.R8, false)
            {
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Bilinear
            };
            var pixels = new Color[NoiseSize * NoiseSize];
            // 平铺采样：Perlin 在 (0,0)/(1,1) 不连续，用双周期采样保证无缝平铺
            float freq = 4f;
            for (int y = 0; y < NoiseSize; y++)
            {
                for (int x = 0; x < NoiseSize; x++)
                {
                    float u = x / (float)NoiseSize;
                    float v = y / (float)NoiseSize;
                    // 4 向混合消除接缝
                    float a = Mathf.PerlinNoise(u * freq, v * freq);
                    float b = Mathf.PerlinNoise((u - 1f) * freq, v * freq);
                    float c = Mathf.PerlinNoise(u * freq, (v - 1f) * freq);
                    float d = Mathf.PerlinNoise((u - 1f) * freq, (v - 1f) * freq);
                    float blend = a * (1 - u) * (1 - v) + b * u * (1 - v)
                                + c * (1 - u) * v + d * u * v;
                    pixels[y * NoiseSize + x] = new Color(blend, 0f, 0f, 1f);
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }
    }
}
