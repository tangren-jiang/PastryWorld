using UnityEngine;
using UnityEngine.InputSystem;

namespace PastryWorld.Input
{
    /// <summary>
    /// 手柄输入提供者。左摇杆控制虚拟光标，南键（A/Cross）为确认。
    /// 技术预判报告 T5。
    /// </summary>
    public class GamepadInputProvider : MonoBehaviour, IInputProvider
    {
        [SerializeField] private float _cursorSpeed = 1200f;

        private Vector2 _virtualPosition;
        private Vector2 _delta;
        private bool _wasPressed;
        private bool _wasReleased;
        private bool _isPressed;

        public Vector2 PointerPosition => _virtualPosition;
        public Vector2 DeltaPosition => _delta;
        public bool IsPressed => _isPressed;
        public bool WasPressedThisFrame => _wasPressed;
        public bool WasReleasedThisFrame => _wasReleased;
        public float Pressure => _isPressed ? 1f : 0f;

        void Awake()
        {
            _virtualPosition = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        }

        void Update()
        {
            var pad = Gamepad.current;
            if (pad == null)
            {
                _delta = Vector2.zero;
                _wasPressed = false;
                _wasReleased = false;
                return;
            }

            var stick = pad.leftStick.ReadValue();
            _delta = stick * _cursorSpeed * Time.deltaTime;
            _virtualPosition += _delta;
            _virtualPosition.x = Mathf.Clamp(_virtualPosition.x, 0, Screen.width);
            _virtualPosition.y = Mathf.Clamp(_virtualPosition.y, 0, Screen.height);

            bool pressed = pad.buttonSouth.isPressed;
            _wasPressed = pad.buttonSouth.wasPressedThisFrame;
            _wasReleased = pad.buttonSouth.wasReleasedThisFrame;
            _isPressed = pressed;
        }

        void OnDisable()
        {
            _delta = Vector2.zero;
            _wasPressed = false;
            _wasReleased = false;
            _isPressed = false;
        }
    }
}
