using UnityEngine;
using UnityEngine.InputSystem;

namespace PastryWorld.Input
{
    /// <summary>
    /// 触摸输入提供者。基于 Input System 的 Touchscreen.current。
    /// </summary>
    public class TouchInputProvider : MonoBehaviour, IInputProvider
    {
        public Vector2 PointerPosition =>
            Touchscreen.current != null ? Touchscreen.current.position.value : Vector2.zero;

        public Vector2 DeltaPosition =>
            Touchscreen.current != null ? Touchscreen.current.delta.value : Vector2.zero;

        public bool IsPressed =>
            Touchscreen.current != null && Touchscreen.current.press.isPressed;

        public bool WasPressedThisFrame =>
            Touchscreen.current != null && Touchscreen.current.press.wasPressedThisFrame;

        public bool WasReleasedThisFrame =>
            Touchscreen.current != null && Touchscreen.current.press.wasReleasedThisFrame;

        public float Pressure =>
            Touchscreen.current != null ? Touchscreen.current.press.value : 0f;
    }
}
