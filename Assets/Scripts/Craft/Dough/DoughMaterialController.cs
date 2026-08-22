using UnityEngine;

namespace PastryWorld.Craft.Dough
{
    /// <summary>
    /// 面团材质控制器。管理 _CookProgress、_Smoothness 等着色器参数。
    /// 技术预判报告 T3/A2：材质属性动画（不使用多张贴图切换）。
    /// </summary>
    [RequireComponent(typeof(MeshRenderer))]
    public class DoughMaterialController : MonoBehaviour
    {
        [Header("配置")]
        [SerializeField] private DoughPhysicsConfig _config;

        private MaterialPropertyBlock _propBlock;
        private MeshRenderer _renderer;

        // 着色器参数 ID（缓存）
        private static readonly int CookProgressID = Shader.PropertyToID("_CookProgress");
        private static readonly int SmoothnessID = Shader.PropertyToID("_Smoothness");
        private static readonly int RawColorID = Shader.PropertyToID("_RawColor");
        private static readonly int CookedColorID = Shader.PropertyToID("_CookedColor");
        private static readonly int BaseColorID = Shader.PropertyToID("_BaseColor");

        // 当前值
        private float _cookProgress;
        private float _smoothness;

        void Awake()
        {
            _renderer = GetComponent<MeshRenderer>();
            _propBlock = new MaterialPropertyBlock();

            float initSmooth = _config != null ? _config.initialSmoothness : 0.1f;
            SetSmoothness(initSmooth);
        }

        /// <summary>
        /// 设置光滑度（0=粗糙, 1=光滑）。越揉越大。
        /// </summary>
        public void SetSmoothness(float value)
        {
            _smoothness = Mathf.Clamp01(value);
            _renderer.GetPropertyBlock(_propBlock);
            _propBlock.SetFloat(SmoothnessID, _smoothness);
            _renderer.SetPropertyBlock(_propBlock);
        }

        /// <summary>
        /// 设置蒸制进度（0=生, 1=熟）。
        /// </summary>
        public void SetCookProgress(float value)
        {
            _cookProgress = Mathf.Clamp01(value);
            _renderer.GetPropertyBlock(_propBlock);
            _propBlock.SetFloat(CookProgressID, _cookProgress);
            _renderer.SetPropertyBlock(_propBlock);
        }

        /// <summary>
        /// 根据揉面圈数更新光滑度。
        /// </summary>
        public void UpdateSmoothnessByKneadCircles(float circles)
        {
            float target = _config != null ? _config.maxSmoothness : 0.9f;
            float init = _config != null ? _config.initialSmoothness : 0.1f;
            float needed = _config != null ? _config.circlesToMaxSmoothness : 3f;

            float t = Mathf.Clamp01(circles / needed);
            SetSmoothness(Mathf.Lerp(init, target, t));
        }

        /// <summary>
        /// 设置颜色覆盖（用于品质反馈）。
        /// </summary>
        public void SetColorTint(Color color)
        {
            _renderer.GetPropertyBlock(_propBlock);
            _propBlock.SetColor(BaseColorID, color);
            _renderer.SetPropertyBlock(_propBlock);
        }

        /// <summary>
        /// 重置材质参数。
        /// </summary>
        public void ResetMaterial()
        {
            float initSmooth = _config != null ? _config.initialSmoothness : 0.1f;
            _cookProgress = 0f;
            _smoothness = initSmooth;
            SetSmoothness(initSmooth);
            SetCookProgress(0f);
            SetColorTint(Color.white);
        }

        public float CookProgress => _cookProgress;
        public float Smoothness => _smoothness;
    }
}
