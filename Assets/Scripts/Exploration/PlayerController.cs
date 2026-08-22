using UnityEngine;
using UnityEngine.InputSystem;

namespace PastryWorld.Exploration
{
    /// <summary>
    /// 玩家控制器。负责移动和交互。
    /// 使用 Rigidbody2D（Kinematic）+ MovePosition 实现平滑移动。
    /// 技术预判报告 T9。
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public class PlayerController : MonoBehaviour
    {
        [Header("移动")]
        [SerializeField] private InputActionAsset _inputActions;
        [SerializeField] private float _moveSpeed = 3f;

        [Header("交互")]
        [SerializeField] private float _interactRange = 1.2f;

        private Rigidbody2D _rb;
        private InputAction _moveAction;
        private InputAction _interactAction;
        private InputAction _cancelAction;
        private Vector2 _facing = Vector2.down;

        public Vector2 Facing => _facing;
        public float MoveSpeed => _moveSpeed;

        void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _rb.bodyType = RigidbodyType2D.Kinematic;
            _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            _rb.sleepMode = RigidbodySleepMode2D.NeverSleep;
        }

        void OnEnable()
        {
            if (_inputActions == null)
            {
                Debug.LogError("[PlayerController] InputActionAsset 未分配", this);
                return;
            }

            var playerMap = _inputActions.FindActionMap("Player");
            if (playerMap == null)
            {
                Debug.LogError("[PlayerController] 未找到 Player action map", this);
                return;
            }

            _moveAction = playerMap.FindAction("Move");
            _interactAction = playerMap.FindAction("Interact");
            _cancelAction = playerMap.FindAction("Cancel");

            _moveAction?.Enable();
            _interactAction?.Enable();
            _cancelAction?.Enable();
        }

        void OnDisable()
        {
            _moveAction?.Disable();
            _interactAction?.Disable();
            _cancelAction?.Disable();
        }

        void FixedUpdate()
        {
            if (_moveAction == null) return;

            Vector2 input = _moveAction.ReadValue<Vector2>();
            if (input.sqrMagnitude > 0.01f)
            {
                // 归一化对角线移动
                if (input.sqrMagnitude > 1f)
                    input.Normalize();

                Vector2 newPos = _rb.position + input * _moveSpeed * Time.fixedDeltaTime;
                _rb.MovePosition(newPos);

                // 记录朝向（四方向）
                if (Mathf.Abs(input.x) > Mathf.Abs(input.y))
                    _facing = input.x > 0 ? Vector2.right : Vector2.left;
                else
                    _facing = input.y > 0 ? Vector2.up : Vector2.down;
            }
        }

        void Update()
        {
            if (TryInteract(out var interactable))
            {
                interactable.OnInteract();
            }
        }

        /// <summary>
        /// 尝试交互。返回是否命中可交互对象。
        /// </summary>
        public bool TryInteract(out IInteractable interactable)
        {
            interactable = null;
            if (_interactAction == null || !_interactAction.WasPressedThisFrame())
                return false;

            Vector2 origin = _rb.position + _facing * _interactRange * 0.5f;
            RaycastHit2D hit = Physics2D.CircleCast(
                origin, _interactRange * 0.5f, _facing, _interactRange * 0.5f,
                LayerMask.GetMask("Interactable"));

            if (hit.collider != null)
            {
                interactable = hit.collider.GetComponent<IInteractable>();
                return interactable != null;
            }
            return false;
        }
    }

    /// <summary>
    /// 可交互对象接口。
    /// </summary>
    public interface IInteractable
    {
        void OnInteract();
        string GetPromptText();
    }
}
