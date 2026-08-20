using UnityEngine;
using UnityEngine.InputSystem;

namespace PastryWorld.Input
{
    /// <summary>
    /// 鼠标输入提供者。基于 Input System 的 Mouse.current。
    /// </summary>
    public class MouseInputProvider : MonoBehaviour, IInputProvider
    {
        public Vector2 PointerPosition => Mouse.current.position.value;

        public Vector2 DeltaPosition => Mouse.current.delta.value;

        public bool IsPressed => Mouse.current.leftButton.isPressed;

        public bool WasPressedThisFrame => Mouse.current.leftButton.wasPressedThisFrame;

        public bool WasReleasedThisFrame => Mouse.current.leftButton.wasReleasedThisFrame;

        public float Pressure => 1f; // 鼠标无压力，固定为1
    }
}
