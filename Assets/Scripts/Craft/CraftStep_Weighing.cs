using UnityEngine;
using PastryWorld.Input;
using PastryWorld.Core;

namespace PastryWorld.Craft
{
    /// <summary>
    /// 称量工艺步骤。玩家按住屏幕倾倒食材，松开时判定重量是否接近目标。
    /// 技术预判报告 T1。
    /// </summary>
    public class CraftStep_Weighing : CraftStep
    {
        [Header("称量参数")]
        [SerializeField] private float _pourRate = 50f; // 每秒倾倒量
        [SerializeField] private float _maxWeight = 200f;

        [Header("引用")]
        [SerializeField] private MonoBehaviour _inputProviderObj;
        [SerializeField] private SpriteRenderer _fillRenderer;

        private IInputProvider _input;
        private float _currentWeight = 0f;
        private float _fillStartY;
        private Vector3 _fillStartScale;

        protected override void Awake()
        {
            base.Awake();
            _input = _inputProviderObj as IInputProvider;

            if (_fillRenderer != null)
            {
                _fillStartScale = _fillRenderer.transform.localScale;
                _fillStartY = _fillRenderer.transform.localPosition.y;
            }
        }

        protected override void OnInputBegin()
        {
            _currentWeight = 0f;
            UpdateFillVisual();
        }

        void Update()
        {
            if (State != StepState.InputActive) return;
            if (_input == null) return;

            // 按住时倾倒
            if (_input.IsPressed)
            {
                _currentWeight += _pourRate * Time.deltaTime;
                _currentWeight = Mathf.Min(_currentWeight, _maxWeight);
                UpdateFillVisual();
            }

            // 松开时提交判定
            if (_input.WasReleasedThisFrame && _currentWeight > 0f)
            {
                SubmitEvaluation();
            }
        }

        protected override float Evaluate()
        {
            if (_qualityEvaluator != null)
            {
                float target = _config != null ? _config.targetValue : 100f;
                float tol = _config != null ? _config.tolerance : 10f;
                return _qualityEvaluator.Evaluate(_currentWeight, target, tol);
            }
            return 0.5f;
        }

        protected override void OnFeedback(float quality, bool success)
        {
            if (_feedbackController != null)
                _feedbackController.PlayFeedback(quality, success);

            Debug.Log($"[Weighing] 重量={_currentWeight:F1} 目标={(_config != null ? _config.targetValue : 100):F1} " +
                      $"品质={quality:F2} {(success ? "成功" : "失败")}");

            CompleteStep();
        }

        private void UpdateFillVisual()
        {
            if (_fillRenderer == null) return;

            float fillRatio = _currentWeight / _maxWeight;
            var scale = _fillStartScale;
            scale.y *= fillRatio;
            _fillRenderer.transform.localScale = scale;
        }

        public override void ResetStep()
        {
            base.ResetStep();
            _currentWeight = 0f;
            UpdateFillVisual();
            if (_feedbackController != null)
                _feedbackController.ResetFeedback();
        }

        public float CurrentWeight => _currentWeight;
    }
}
