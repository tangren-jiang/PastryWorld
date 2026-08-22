using PastryWorld.Core;
using UnityEngine;

namespace PastryWorld.Exploration
{
    /// <summary>
    /// 弱引导 L1 环境微光效果（T10）。
    /// 挂在场景中的环境粒子物体上（萤光/香气粒子），
    /// 引导等级提升时粒子发射量增强，等级恢复时还原。
    /// 纯世界层表现，无 UI 元素。
    /// </summary>
    [RequireComponent(typeof(ParticleSystem))]
    public class GuidanceAmbienceEffect : MonoBehaviour
    {
        [Tooltip("L1 时粒子发射量倍率（技术预判报告：香气粒子 +50%）")]
        [SerializeField] private float _enhancedEmissionMultiplier = 1.5f;

        [Tooltip("L1 时粒子尺寸倍率（微光更明显）")]
        [SerializeField] private float _enhancedSizeMultiplier = 1.2f;

        private ParticleSystem _particles;
        private ParticleSystem.EmissionModule _emission;
        private ParticleSystem.MainModule _main;
        private float _baseEmissionRate;
        private float _baseStartSize;
        private IEventBus _eventBus;

        void Awake()
        {
            _particles = GetComponent<ParticleSystem>();
            _emission = _particles.emission;
            _main = _particles.main;
            _baseEmissionRate = _emission.rateOverTime.constant;
            _baseStartSize = _main.startSize.constant;
        }

        void OnEnable()
        {
            _eventBus = EventBus.Default;
            _eventBus.Subscribe<GuidanceLevelChangedEvent>(OnGuidanceLevelChanged);
        }

        void OnDisable()
        {
            _eventBus?.Unsubscribe<GuidanceLevelChangedEvent>(OnGuidanceLevelChanged);
        }

        private void OnGuidanceLevelChanged(GuidanceLevelChangedEvent evt)
        {
            if (evt.level >= 1)
            {
                _emission.rateOverTime = _baseEmissionRate * _enhancedEmissionMultiplier;
                _main.startSize = _baseStartSize * _enhancedSizeMultiplier;
            }
            else
            {
                _emission.rateOverTime = _baseEmissionRate;
                _main.startSize = _baseStartSize;
            }
        }
    }
}
