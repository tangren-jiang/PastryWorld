using System;
using UnityEngine;
using PastryWorld.Core;

namespace PastryWorld.Craft
{
    /// <summary>
    /// 工艺步骤状态。
    /// </summary>
    public enum StepState
    {
        Idle,       // 未开始
        InputActive, // 玩家正在操作
        Evaluating,  // 正在判定
        Feedback,    // 反馈播放中
        Completed    // 已完成
    }

    /// <summary>
    /// 工艺输入模式。不同工艺用不同输入方式。
    /// </summary>
    public enum CraftInputType
    {
        Drag,       // 拖拽（称量：拖动容器倾倒）
        Circular,   // 循环往复（揉面：画圈）
        Timing,     // 时机（蒸制：等待+按键）
        Placement   // 放置（装饰：拖到目标位置）
    }

    /// <summary>
    /// 制作步骤抽象基类。统一三段式架构：OnInputBegin → Evaluate → OnFeedback。
    /// 技术预判报告 T1。
    /// </summary>
    public abstract class CraftStep : MonoBehaviour
    {
        [Header("步骤标识")]
        [SerializeField] protected string _stepId;
        [SerializeField] protected bool _isMiniStep = false;

        [Header("配置")]
        [SerializeField] protected StepConfig _config;
        [SerializeField] protected CraftInputType _inputType = CraftInputType.Drag;

        // 组件
        protected QualityEvaluator _qualityEvaluator;
        protected FeedbackController _feedbackController;

        // 状态
        public StepState State { get; protected set; } = StepState.Idle;
        public string StepId => _stepId;
        public bool IsMiniStep => _isMiniStep;
        public StepConfig Config => _config;
        public CraftInputType InputType => _inputType;

        // 结果
        public float QualityScore { get; protected set; }
        public bool LastResultSuccess { get; protected set; }

        // 事件
        public event Action<CraftStep> OnStepStarted;
        public event Action<CraftStep, float> OnStepCompleted;

        /// <summary>
        /// 步骤开始。由 CraftManager 调用。
        /// </summary>
        public virtual void BeginStep()
        {
            if (State != StepState.Idle)
            {
                Debug.LogWarning($"[CraftStep] 步骤 {_stepId} 不在 Idle 状态，无法开始");
                return;
            }

            State = StepState.InputActive;
            OnStepStarted?.Invoke(this);
            OnInputBegin();
        }

        /// <summary>
        /// 第一阶段：输入开始。子类重写以初始化输入监听。
        /// </summary>
        protected abstract void OnInputBegin();

        /// <summary>
        /// 第二阶段：判定。子类重写以计算品质分数（0-1）。
        /// </summary>
        protected abstract float Evaluate();

        /// <summary>
        /// 第三阶段：反馈。子类重写以播放成功/失败反馈。
        /// </summary>
        protected abstract void OnFeedback(float quality, bool success);

        /// <summary>
        /// 子类调用：输入阶段结束，进入判定。
        /// </summary>
        protected void SubmitEvaluation()
        {
            if (State != StepState.InputActive) return;

            State = StepState.Evaluating;
            QualityScore = Evaluate();

            // 判定成功/失败
            float threshold = _config != null ? _config.passThreshold : 0.5f;
            LastResultSuccess = QualityScore >= threshold;

            State = StepState.Feedback;
            OnFeedback(QualityScore, LastResultSuccess);

            // 通知管理器
            OnStepCompleted?.Invoke(this, QualityScore);
        }

        /// <summary>
        /// 反馈播放完毕。子类调用以完成步骤。
        /// </summary>
        protected void CompleteStep()
        {
            State = StepState.Completed;
        }

        /// <summary>
        /// 重置步骤到 Idle。
        /// </summary>
        public virtual void ResetStep()
        {
            State = StepState.Idle;
            QualityScore = 0f;
            LastResultSuccess = false;
        }

        protected virtual void Awake()
        {
            _qualityEvaluator = GetComponent<QualityEvaluator>();
            _feedbackController = GetComponent<FeedbackController>();
        }
    }
}
