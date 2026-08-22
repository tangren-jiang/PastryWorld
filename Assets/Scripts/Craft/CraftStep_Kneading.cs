using UnityEngine;
using PastryWorld.Input;
using PastryWorld.Core;

namespace PastryWorld.Craft
{
    /// <summary>
    /// 揉面工艺步骤。玩家画圆圈揉面，累计角度判定。
    /// 技术预判报告 T1。
    /// </summary>
    public class CraftStep_Kneading : CraftStep
    {
        [Header("揉面参数")]
        [SerializeField] private float _anglePerUnit = 360f; // 完成一圈需要的角度
        [SerializeField] private float _maxCircles = 5f;     // 最多计算圈数
        [SerializeField] private float _minRadius = 30f;     // 最小有效画圈半径（像素）

        [Header("引用")]
        [SerializeField] private MonoBehaviour _inputProviderObj;

        private IInputProvider _input;
        private Vector2 _lastPos;
        private Vector2 _centerPos;
        private float _accumulatedAngle;
        private bool _hasLastPos;
        private bool _hasCenter;

        protected override void Awake()
        {
            base.Awake();
            _input = _inputProviderObj as IInputProvider;
        }

        protected override void OnInputBegin()
        {
            _accumulatedAngle = 0f;
            _hasLastPos = false;
            _hasCenter = false;
        }

        void Update()
        {
            if (State != StepState.InputActive) return;
            if (_input == null) return;

            if (_input.IsPressed)
            {
                Vector2 currentPos = _input.PointerPosition;

                // 首次按下时记录圆心位置
                if (!_hasCenter)
                {
                    _centerPos = currentPos;
                    _hasCenter = true;
                }

                if (_hasLastPos)
                {
                    Vector2 delta = currentPos - _lastPos;
                    // 过滤过小位移和离圆心太近的点（抖动不算）
                    float distFromCenter = (currentPos - GetCenter()).magnitude;
                    if (delta.magnitude > 1f && distFromCenter >= _minRadius)
                    {
                        // 累计角度（基于位移方向变化）
                        float angleDelta = Vector2.SignedAngle(_lastPos - GetCenter(), currentPos - GetCenter());
                        if (Mathf.Abs(angleDelta) < 180f) // 过滤跳变
                        {
                            _accumulatedAngle += Mathf.Abs(angleDelta);
                        }
                    }
                }

                _lastPos = currentPos;
                _hasLastPos = true;
            }
            else if (_input.WasReleasedThisFrame && _accumulatedAngle > 0f)
            {
                SubmitEvaluation();
            }
        }

        private Vector2 GetCenter()
        {
            // 使用玩家首次按下位置作为揉面圆心
            return _centerPos;
        }

        protected override float Evaluate()
        {
            float targetCircles = _config != null ? _config.targetValue : 3f;
            float actualCircles = _accumulatedAngle / _anglePerUnit;
            actualCircles = Mathf.Min(actualCircles, _maxCircles);

            if (_qualityEvaluator != null)
            {
                float target = targetCircles * _anglePerUnit;
                float tol = (_config != null ? _config.tolerance : 1f) * _anglePerUnit;
                return _qualityEvaluator.Evaluate(_accumulatedAngle, target, tol);
            }
            return 0.5f;
        }

        protected override void OnFeedback(float quality, bool success)
        {
            if (_feedbackController != null)
                _feedbackController.PlayFeedback(quality, success);

            float circles = _accumulatedAngle / _anglePerUnit;
            float target = _config != null ? _config.targetValue : 3f;
            Debug.Log($"[Kneading] 圈数={circles:F2} 目标={target:F1} " +
                      $"品质={quality:F2} {(success ? "成功" : "失败")}");

            CompleteStep();
        }

        public override void ResetStep()
        {
            base.ResetStep();
            _accumulatedAngle = 0f;
            _hasLastPos = false;
            _hasCenter = false;
        }

        public float AccumulatedCircles => _accumulatedAngle / _anglePerUnit;
    }
}
