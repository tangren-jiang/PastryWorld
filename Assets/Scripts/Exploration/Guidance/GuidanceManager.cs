using PastryWorld.Core;
using UnityEngine;

namespace PastryWorld.Exploration
{
    /// <summary>
    /// 弱引导管理器（T10）。
    /// 累计无有效交互时间，达到阈值后提升引导等级并广播事件。
    /// 引导效果全部由世界层消费方实现（环境微光、香气粒子等），
    /// 本类只负责计时与分级，不做任何 UI 表现。
    ///
    /// 有效交互定义（技术预判报告 T10 对齐修订）：
    /// ①与可交互物体交互（PlayerInteractedEvent 自动重置）
    /// ②NPC 对话（同①，通过 OnInteract 链路）
    /// ③进入新子区域 / 打开图鉴等（调用 MarkInteraction()）
    /// 探索移动不算交互。
    /// </summary>
    public class GuidanceManager : MonoBehaviour
    {
        [Header("引导分级（Demo 只做 L0+L1）")]
        [Tooltip("无有效交互多久后进入 L1（环境微光提示增强），秒")]
        [SerializeField] private float _level1Threshold = 180f;

        private float _timeSinceLastInteraction;
        private int _currentLevel;
        private IEventBus _eventBus;

        /// <summary>当前引导等级（0=正常，1=微光提示增强）。</summary>
        public int CurrentLevel => _currentLevel;

        /// <summary>距上次有效交互的时长（秒）。</summary>
        public float TimeSinceLastInteraction => _timeSinceLastInteraction;

        void Awake()
        {
            _eventBus = EventBus.Default;
        }

        void OnEnable()
        {
            _eventBus.Subscribe<PlayerInteractedEvent>(OnPlayerInteracted);
        }

        void OnDisable()
        {
            _eventBus.Unsubscribe<PlayerInteractedEvent>(OnPlayerInteracted);
        }

        void Update()
        {
            // timeScale=0（暂停）时 deltaTime 为 0，计时自动暂停
            _timeSinceLastInteraction += Time.deltaTime;

            if (_currentLevel == 0 && _timeSinceLastInteraction >= _level1Threshold)
            {
                SetLevel(1);
            }
        }

        /// <summary>
        /// 外部标记一次有效交互（进入新子区域、打开图鉴、制作完成等）。
        /// 制作模块（Craft）与本模块无程序集引用，由消费方调用此方法。
        /// </summary>
        public void MarkInteraction()
        {
            _timeSinceLastInteraction = 0f;
            if (_currentLevel != 0)
            {
                SetLevel(0);
            }
        }

        private void OnPlayerInteracted(PlayerInteractedEvent evt)
        {
            MarkInteraction();
        }

        private void SetLevel(int level)
        {
            _currentLevel = level;
            _eventBus.Publish(new GuidanceLevelChangedEvent
            {
                level = level,
                idleTime = _timeSinceLastInteraction
            });
            Debug.Log($"[GuidanceManager] 引导等级 → L{level}（无交互 {_timeSinceLastInteraction:F0}s）", this);
        }

        /// <summary>测试/调试用：直接设置无交互时长。</summary>
        public void DebugSetIdleTime(float seconds)
        {
            _timeSinceLastInteraction = seconds;
        }
    }
}
