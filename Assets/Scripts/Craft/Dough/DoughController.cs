using UnityEngine;

namespace PastryWorld.Craft.Dough
{
    /// <summary>
    /// 面团总控制器。集成网格形变、材质控制、蒸汽粒子、弹性回弹。
    /// 对外提供简洁接口供 CraftStep_Kneading/Steaming 调用。
    /// 技术预判报告 T3。
    /// </summary>
    public class DoughController : MonoBehaviour
    {
        [Header("组件引用")]
        [SerializeField] private DoughMeshDeformer _deformer;
        [SerializeField] private DoughMaterialController _material;
        [SerializeField] private SteamParticleController _steam;
        [SerializeField] private DoughBounceEffect _bounce;
        [SerializeField] private DoughPhysicsConfig _config;

        // 揉面进度（圈数）
        private float _kneadCircles;

        /// <summary>
        /// 揉面拖拽。在 CraftStep_Kneading.Update 中每帧调用。
        /// </summary>
        public void OnKneadDrag(Vector2 worldPosition, Vector2 deltaPosition)
        {
            if (_deformer != null)
            {
                _deformer.ApplyDrag(worldPosition, deltaPosition);
            }
        }

        /// <summary>
        /// 更新揉面进度（影响光滑度）。
        /// </summary>
        public void UpdateKneadProgress(float circles)
        {
            _kneadCircles = circles;
            if (_material != null)
            {
                _material.UpdateSmoothnessByKneadCircles(circles);
            }
        }

        /// <summary>
        /// 开始蒸制。
        /// </summary>
        public void StartSteaming()
        {
            if (_steam != null)
            {
                _steam.StartSteam();
            }
        }

        /// <summary>
        /// 更新蒸制进度（0-1）。
        /// </summary>
        public void UpdateCookProgress(float progress)
        {
            if (_material != null)
            {
                _material.SetCookProgress(progress);
            }
            if (_steam != null)
            {
                _steam.SetIntensity(progress);
            }
        }

        /// <summary>
        /// 蒸制完成。播放弹性回弹 + 颜色定格。
        /// </summary>
        public void FinishSteaming(float finalProgress)
        {
            UpdateCookProgress(finalProgress);
            if (_steam != null)
            {
                _steam.StopSteam();
            }
            if (_bounce != null)
            {
                _bounce.PlayBounce();
            }
        }

        /// <summary>
        /// 设置颜色覆盖（品质反馈）。
        /// </summary>
        public void SetColorTint(Color color)
        {
            if (_material != null)
            {
                _material.SetColorTint(color);
            }
        }

        /// <summary>
        /// 重置面团到初始状态。
        /// </summary>
        public void ResetDough()
        {
            if (_deformer != null)
            {
                _deformer.ResetBones();
            }
            if (_material != null)
            {
                _material.ResetMaterial();
            }
            if (_steam != null)
            {
                _steam.StopSteam();
            }
            _kneadCircles = 0f;
        }

        public float DeformationAmount => _deformer != null ? _deformer.GetDeformationAmount() : 0f;
        public float KneadCircles => _kneadCircles;
    }
}
