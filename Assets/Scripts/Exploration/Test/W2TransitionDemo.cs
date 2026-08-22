using UnityEngine;

namespace PastryWorld.Exploration.Test
{
    /// <summary>
    /// W2 测试演示驱动（T10/T11/T12 快捷验证）。
    /// T = 触发水墨转场；G = 快进引导计时至 L1。
    /// </summary>
    public class W2TransitionDemo : MonoBehaviour
    {
        void Update()
        {
            if (UnityEngine.Input.GetKeyDown(KeyCode.T))
            {
                InkTransitionController.Transition(() =>
                {
                    Debug.Log("[W2Demo] 墨迹已覆盖屏幕（此处执行场景切换/黑屏动作）");
                });
            }

            // 快进引导计时（验证 T10 不用等 180 秒）
            if (UnityEngine.Input.GetKeyDown(KeyCode.G))
            {
                var gm = FindFirstObjectByType<GuidanceManager>();
                if (gm != null)
                {
                    gm.DebugSetIdleTime(999f);
                    Debug.Log("[W2Demo] 引导计时快进至 999s，下一帧进入 L1");
                }
            }
        }
    }
}
