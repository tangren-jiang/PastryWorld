using UnityEngine;
using PastryWorld.Input;
using PastryWorld.Core;

namespace PastryWorld.Craft
{
    /// <summary>
    /// 装饰工艺步骤。玩家拖拽装饰物到目标位置。
    /// 技术预判报告 T1。
    /// </summary>
    public class CraftStep_Decorating : CraftStep
    {
        [Header("装饰参数")]
        [SerializeField] private Transform[] _targetSlots;  // 目标位置
        [SerializeField] private float _snapRadius = 0.5f;   // 吸附半径
        [SerializeField] private GameObject[] _decorationPrefabs; // 装饰物预制

        [Header("引用")]
        [SerializeField] private MonoBehaviour _inputProviderObj;

        private IInputProvider _input;
        private int _currentSlotIndex = 0;
        private int _placedCount = 0;
        private bool _waitingForRelease; // 防止同一拖拽连续吸附多个槽位

        protected override void Awake()
        {
            base.Awake();
            _input = _inputProviderObj as IInputProvider;
        }

        protected override void OnInputBegin()
        {
            _currentSlotIndex = 0;
            _placedCount = 0;
            _waitingForRelease = false;
        }

        void Update()
        {
            if (State != StepState.InputActive) return;
            if (_input == null || _targetSlots == null || _targetSlots.Length == 0) return;

            if (_currentSlotIndex >= _targetSlots.Length)
            {
                SubmitEvaluation();
                return;
            }

            // 玩家拖拽中
            if (_input.IsPressed)
            {
                // 上一次吸附后需要松手才能吸附下一个
                if (_waitingForRelease) return;

                Vector2 worldPos = GetWorldPosition(_input.PointerPosition);
                Transform target = _targetSlots[_currentSlotIndex];

                float dist = Vector2.Distance(worldPos, target.position);
                if (dist < _snapRadius)
                {
                    // 吸附到目标
                    _placedCount++;
                    _currentSlotIndex++;
                    _waitingForRelease = true;

                    if (_currentSlotIndex >= _targetSlots.Length)
                    {
                        SubmitEvaluation();
                    }
                }
            }
            else if (_input.WasReleasedThisFrame)
            {
                _waitingForRelease = false;
            }
        }

        private Vector2 GetWorldPosition(Vector2 screenPos)
        {
            Camera cam = Camera.main;
            if (cam == null) return Vector2.zero;
            return cam.ScreenToWorldPoint(screenPos);
        }

        protected override float Evaluate()
        {
            int total = _targetSlots != null ? _targetSlots.Length : 0;
            if (total == 0) return 0.5f;

            float ratio = (float)_placedCount / total;

            if (_qualityEvaluator != null)
            {
                return _qualityEvaluator.EvaluateRaw(ratio);
            }
            return ratio;
        }

        protected override void OnFeedback(float quality, bool success)
        {
            if (_feedbackController != null)
                _feedbackController.PlayFeedback(quality, success);

            Debug.Log($"[Decorating] 放置={_placedCount}/{_targetSlots.Length} " +
                      $"品质={quality:F2} {(success ? "成功" : "失败")}");

            CompleteStep();
        }

        public override void ResetStep()
        {
            base.ResetStep();
            _currentSlotIndex = 0;
            _placedCount = 0;
            _waitingForRelease = false;
        }

        public int PlacedCount => _placedCount;
        public int TotalSlots => _targetSlots != null ? _targetSlots.Length : 0;
    }
}
