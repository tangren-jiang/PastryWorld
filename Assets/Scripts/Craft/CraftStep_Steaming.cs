using UnityEngine;
using PastryWorld.Input;
using PastryWorld.Core;

namespace PastryWorld.Craft
{
    /// <summary>
    /// 蒸制工艺步骤。玩家等待蒸制，在恰当时机按键完成。
    /// 技术预判报告 T1。蒸制着色器在 T3 实现。
    /// </summary>
    public class CraftStep_Steaming : CraftStep
    {
        [Header("蒸制参数")]
        [SerializeField] private float _optimalTime = 5f;    // 最佳蒸制时间（秒）
        [SerializeField] private float _toleranceTime = 1f;  // 容差时间（秒）
        [SerializeField] private float _maxTime = 30f;       // 最大时间（超时自动失败）
        [SerializeField] private bool _requireButton = true;  // 是否需要按键确认

        [Header("引用")]
        [SerializeField] private MonoBehaviour _inputProviderObj;
        [SerializeField] private SpriteRenderer _steamRenderer; // 蒸汽可视化

        private IInputProvider _input;
        private float _elapsedTime;
        private bool _completed;

        protected override void Awake()
        {
            base.Awake();
            _input = _inputProviderObj as IInputProvider;
        }

        protected override void OnInputBegin()
        {
            _elapsedTime = 0f;
            _completed = false;
        }

        void Update()
        {
            if (State != StepState.InputActive) return;

            _elapsedTime += Time.deltaTime;

            // 蒸汽效果（简化：随时间变透明度）
            if (_steamRenderer != null)
            {
                float t = Mathf.Clamp01(_elapsedTime / _optimalTime);
                var color = _steamRenderer.color;
                color.a = t * 0.8f;
                _steamRenderer.color = color;
            }

            // 玩家按键确认完成
            if (_requireButton && _input != null && _input.WasPressedThisFrame)
            {
                _completed = true;
                SubmitEvaluation();
                return;
            }

            // 超时自动判定
            if (_elapsedTime >= _maxTime)
            {
                _completed = true;
                SubmitEvaluation();
                return;
            }

            // 非按键模式：到达最佳时间自动完成
            if (!_requireButton && _elapsedTime >= _optimalTime)
            {
                _completed = true;
                SubmitEvaluation();
            }
        }

        protected override float Evaluate()
        {
            // 评估：时间越接近 _optimalTime 越好
            if (_qualityEvaluator != null)
            {
                return _qualityEvaluator.Evaluate(_elapsedTime, _optimalTime, _toleranceTime);
            }

            float diff = Mathf.Abs(_elapsedTime - _optimalTime);
            return Mathf.Clamp01(1f - diff / _toleranceTime);
        }

        protected override void OnFeedback(float quality, bool success)
        {
            if (_feedbackController != null)
                _feedbackController.PlayFeedback(quality, success);

            Debug.Log($"[Steaming] 时间={_elapsedTime:F1}s 最佳={_optimalTime:F1}s " +
                      $"品质={quality:F2} {(success ? "成功" : "失败")}");

            CompleteStep();
        }

        public override void ResetStep()
        {
            base.ResetStep();
            _elapsedTime = 0f;
            _completed = false;
            if (_steamRenderer != null)
            {
                var color = _steamRenderer.color;
                color.a = 0f;
                _steamRenderer.color = color;
            }
        }

        public float ElapsedTime => _elapsedTime;
        public float Progress => Mathf.Clamp01(_elapsedTime / _optimalTime);
    }
}
