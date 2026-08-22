using UnityEngine;

namespace PastryWorld.Craft.Dough
{
    /// <summary>
    /// 蒸汽粒子系统控制器。
    /// 技术预判报告 T3：蒸制颜色渐变 + 水汽粒子叠加。
    /// </summary>
    [RequireComponent(typeof(ParticleSystem))]
    public class SteamParticleController : MonoBehaviour
    {
        [Header("配置")]
        [Tooltip("最大粒子发射率")]
        [SerializeField] private float _maxEmissionRate = 30f;

        [Tooltip("最小粒子发射率（蒸制开始时）")]
        [SerializeField] private float _minEmissionRate = 5f;

        private ParticleSystem _particles;
        private ParticleSystem.EmissionModule _emission;

        void Awake()
        {
            _particles = GetComponent<ParticleSystem>();
            _emission = _particles.emission;
        }

        void Start()
        {
            // 初始不发射
            SetIntensity(0f);
        }

        /// <summary>
        /// 设置蒸汽强度（0=无, 1=最大）。
        /// </summary>
        public void SetIntensity(float intensity)
        {
            intensity = Mathf.Clamp01(intensity);
            if (intensity <= 0f)
            {
                _emission.rateOverTime = 0f;
            }
            else
            {
                float rate = Mathf.Lerp(_minEmissionRate, _maxEmissionRate, intensity);
                _emission.rateOverTime = rate;
            }
        }

        /// <summary>
        /// 开始蒸汽。
        /// </summary>
        public void StartSteam()
        {
            if (!_particles.isPlaying)
            {
                _particles.Play();
            }
            SetIntensity(0.2f);
        }

        /// <summary>
        /// 停止蒸汽。
        /// </summary>
        public void StopSteam()
        {
            SetIntensity(0f);
            if (_particles.isPlaying)
            {
                _particles.Stop();
            }
        }
    }
}
