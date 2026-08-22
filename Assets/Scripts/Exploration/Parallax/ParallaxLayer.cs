using UnityEngine;

namespace PastryWorld.Exploration
{
    /// <summary>
    /// 视差层（T11）。
    /// 挂在带 SpriteRenderer 的背景层物体上，
    /// 每帧按相机位移 × parallaxFactor 跟随相机移动。
    /// factor=1 完全跟随相机（无穷远，如天空），
    /// factor=0 固定在世界中（等同前景物体）。
    /// 远景用大 factor（0.8-0.95），中景 0.5 左右，近景小 factor（0.1-0.3）。
    /// </summary>
    [DefaultExecutionOrder(100)] // 确保在 ParallaxCamera（默认顺序）移动之后执行
    public class ParallaxLayer : MonoBehaviour
    {
        public enum LayerTier
        {
            /// <summary>核心层：天空/远山/中景/近景（所有平台保留）。</summary>
            Essential,
            /// <summary>装饰层：纯氛围层（移动端降级时隐藏）。</summary>
            Decorative
        }

        [Header("视差")]
        [Tooltip("视差系数 0-1。1=无穷远（贴相机），0=固定世界（前景）。远景大、近景小")]
        [SerializeField, Range(0f, 1f)] private float _parallaxFactor = 0.5f;

        [Tooltip("X 轴参与视差")]
        [SerializeField] private bool _axisX = true;
        [Tooltip("Y 轴参与视差")]
        [SerializeField] private bool _axisY = true;

        [Header("平台降级")]
        [Tooltip("层级档位。Decorative 层在移动端隐藏（技术预判报告：移动端降为 4-5 层）")]
        [SerializeField] private LayerTier _tier = LayerTier.Essential;

        private Camera _camera;
        private Vector3 _lastCameraPos;

        /// <summary>视差系数（运行时可调，调参用）。</summary>
        public float ParallaxFactor => _parallaxFactor;

        void Start()
        {
            _camera = Camera.main;
            if (_camera != null)
            {
                _lastCameraPos = _camera.transform.position;
            }

            // 移动端降级：隐藏装饰层
            if (_tier == LayerTier.Decorative && Application.isMobilePlatform)
            {
                gameObject.SetActive(false);
            }
        }

        void LateUpdate()
        {
            if (_camera == null) return;

            Vector3 camPos = _camera.transform.position;
            Vector3 delta = camPos - _lastCameraPos;
            _lastCameraPos = camPos;

            Vector3 move = Vector3.zero;
            if (_axisX) move.x = delta.x * _parallaxFactor;
            if (_axisY) move.y = delta.y * _parallaxFactor;
            transform.position += move;
        }
    }
}
