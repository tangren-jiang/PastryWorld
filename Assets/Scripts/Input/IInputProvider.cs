using UnityEngine;

namespace PastryWorld.Input
{
    /// <summary>
    /// 输入提供者抽象接口。上层系统只依赖此接口，不关心具体设备。
    /// 技术预判报告 T5。
    /// </summary>
    public interface IInputProvider
    {
        /// <summary>当前指针位置（屏幕坐标）</summary>
        Vector2 PointerPosition { get; }

        /// <summary>每帧位移量（用于判断拖拽速度）</summary>
        Vector2 DeltaPosition { get; }

        /// <summary>当前是否按下</summary>
        bool IsPressed { get; }

        /// <summary>本帧是否刚按下</summary>
        bool WasPressedThisFrame { get; }

        /// <summary>本帧是否刚释放</summary>
        bool WasReleasedThisFrame { get; }

        /// <summary>压力值 0-1（触摸=1或按面积，鼠标=1）</summary>
        float Pressure { get; }
    }
}
