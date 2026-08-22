using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace PastryWorld.Input
{
    /// <summary>
    /// 输入管理器。根据最后使用的设备自动切换 IInputProvider。
    /// 技术预判报告 T5。
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class InputManager : MonoBehaviour
    {
        public static InputManager Instance { get; private set; }

        [SerializeField] private MouseInputProvider _mouseProvider;
        [SerializeField] private TouchInputProvider _touchProvider;
        [SerializeField] private GamepadInputProvider _gamepadProvider;

        private IInputProvider _activeProvider;
        private InputDevice _lastDevice;

        // 线程安全：onEvent 可能在非主线程调用，排队到 Update 处理
        private volatile int _pendingDeviceType; // 0=None, 1=Mouse, 2=Touch, 3=Gamepad

        public IInputProvider ActiveProvider => _activeProvider;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        void OnEnable()
        {
            InputSystem.onEvent += OnInputEvent;
        }

        void OnDisable()
        {
            InputSystem.onEvent -= OnInputEvent;
        }

        void Start()
        {
            // 初始选择：有鼠标用鼠标，否则有手柄用手柄，否则触摸
            if (Mouse.current != null)
                SetActiveProvider(_mouseProvider);
            else if (Gamepad.current != null)
                SetActiveProvider(_gamepadProvider);
            else
                SetActiveProvider(_touchProvider);
        }

        void Update()
        {
            // 在主线程处理排队的设备切换
            if (_pendingDeviceType != 0)
            {
                switch (_pendingDeviceType)
                {
                    case 1: SetActiveProvider(_mouseProvider); break;
                    case 2: SetActiveProvider(_touchProvider); break;
                    case 3: SetActiveProvider(_gamepadProvider); break;
                }
                _pendingDeviceType = 0;
            }
        }

        private void OnInputEvent(InputEventPtr eventPtr, InputDevice device)
        {
            // 注意：此回调可能在非主线程调用，只做轻量的排队操作
            if (device is Mouse)
                _pendingDeviceType = 1;
            else if (device is Touchscreen)
                _pendingDeviceType = 2;
            else if (device is Gamepad)
                _pendingDeviceType = 3;
        }

        private void SetActiveProvider(IInputProvider provider)
        {
            if (provider == null)
                return;
            _activeProvider = provider;
        }
    }
}
