using UnityEngine;
using PastryWorld.Input;
using PastryWorld.Core;
using PastryWorld.Craft.Dough;

namespace PastryWorld.Craft
{
    /// <summary>
    /// 蒸制工艺步骤。玩家等待蒸制，在恰当时机按键完成。
    /// T1 工艺架构 + T3 蒸制着色器/蒸汽粒子集成。
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
        [SerializeField] private SpriteRenderer _steamRenderer; // 蒸汽可视化（旧版兼容）
        [SerializeField] private DoughController _doughController; // T3 面团控制器

        private IInputProvider _input;
        private float _elapsedTime;

        protected override void Awake()
        {
            base.Awake();
            _input = _inputProviderObj as IInputProvider;
        }

        protected override void OnInputBegin()
        {
            _elapsedTime = 0f;
            if (_doughController != null)
                _doughController.StartSteaming();
        }

        void Update()
        {
            if (State != StepState.InputActive) return;

            _elapsedTime += Time.deltaTime;

            // 蒸汽效果（旧版兼容：SpriteRenderer 透明度）
            if (_steamRenderer != null)
            {
                float t = Mathf.Clamp01(_elapsedTime / _optimalTime);
                var color = _steamRenderer.color;
                color.a = t * 0.8f;
                _steamRenderer.color = color;
            }

            // T3: 面团蒸制进度（着色器颜色渐变 + 蒸汽粒子）
            if (_doughController != null)
            {
                float cookProgress = Mathf.Clamp01(_elapsedTime / _optimalTime);
                _doughController.UpdateCookProgress(cookProgress);
            }

            // 玩家按键确认完成
            if (_requireButton && _input != null && _input.WasPressedThisFrame)
            {
                SubmitEvaluation();
                return;
            }

            // 超时自动判定
            if (_elapsedTime >= _maxTime)
            {
                SubmitEvaluation();
                return;
            }

            // 非按键模式：到达最佳时间自动完成
            if (!_requireButton && _elapsedTime >= _optimalTime)
            {
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

            // T3: 蒸制完成，播放弹性回弹 + 颜色定格
            if (_doughController != null)
            {
                float finalProgress = success ? Mathf.Clamp01(_elapsedTime / _optimalTime) : 1f;
                _doughController.FinishSteaming(finalProgress);
            }

            Debug.Log($"[Steaming] 时间={_elapsedTime:F1}s 最佳={_optimalTime:F1}s " +
                      $"品质={quality:F2} {(success ? "成功" : "失败")}");

            CompleteStep();
        }

        public override void ResetStep()
        {
            base.ResetStep();
            _elapsedTime = 0f;
            if (_steamRenderer != null)
            {
                var color = _steamRenderer.color;
                color.a = 0f;
                _steamRenderer.color = color;
            }
            if (_doughController != null)
            {
                _doughController.ResetDough();
            }
        }

        public float ElapsedTime => _elapsedTime;
        public float Progress => Mathf.Clamp01(_elapsedTime / _optimalTime);
    }
}
