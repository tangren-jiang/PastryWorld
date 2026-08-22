using UnityEngine;

namespace PastryWorld.Craft.Dough
{
    /// <summary>
    /// 面团物理参数配置。ScriptableObject 数据驱动，方便调参不改代码。
    /// 技术预判报告 T3：弹簧参数 SO 化（对齐项 3 追加）。
    /// </summary>
    [CreateAssetMenu(fileName = "DoughPhysicsConfig_", menuName = "PastryWorld/Dough Physics Config")]
    public class DoughPhysicsConfig : ScriptableObject
    {
        [Header("弹簧参数")]
        [Tooltip("弹簧刚度（越大回弹越快）")]
        [Range(5f, 200f)] public float stiffness = 50f;

        [Tooltip("阻尼系数（越大振荡衰减越快）")]
        [Range(1f, 50f)] public float damping = 12f;

        [Tooltip("最大位移距离（世界单位）")]
        public float maxDisplacement = 0.8f;

        [Header("揉面响应")]
        [Tooltip("拖拽力转换系数（指针位移→骨骼位移）")]
        [Range(0.1f, 2f)] public float dragInfluence = 0.5f;

        [Tooltip("影响半径（拖拽影响周围骨骼的范围，世界单位）")]
        public float influenceRadius = 1.5f;

        [Tooltip("影响曲线衰减（0=线性, 1=陡峭衰减）")]
        [Range(0f, 1f)] public float influenceFalloff = 0.5f;

        [Header("网格参数")]
        [Tooltip("网格分辨率（每行顶点数）")]
        [Range(4, 32)] public int meshResolution = 12;

        [Tooltip("面团半径（世界单位）")]
        public float doughRadius = 2f;

        [Header("材质参数")]
        [Tooltip("初始光滑度（越揉越大）")]
        [Range(0f, 1f)] public float initialSmoothness = 0.1f;

        [Tooltip("最大光滑度（完全揉透后）")]
        [Range(0f, 1f)] public float maxSmoothness = 0.9f;

        [Tooltip("达到最大光滑度所需的揉面圈数")]
        public float circlesToMaxSmoothness = 3f;

        [Header("蒸制参数")]
        [Tooltip("生面团颜色")]
        public Color rawColor = new Color(0.82f, 0.72f, 0.55f);

        [Tooltip("蒸熟面团颜色")]
        public Color cookedColor = new Color(0.95f, 0.88f, 0.72f);

        [Tooltip("蒸糯弹性回弹强度")]
        public float bounceScale = 1.05f;

        [Tooltip("蒸糯弹性回弹时长（秒）")]
        public float bounceDuration = 0.3f;
    }
}
