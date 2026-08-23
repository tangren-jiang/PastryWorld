using UnityEngine;

namespace PastryWorld.Reality.Test
{
    /// <summary>
    /// W4 现实段落测试驱动（T17+T20）。
    /// F=直接播第一段独白（书桌） C=打印当前选择旗标
    /// （正常路径：走近物件按 E 交互；电话对话选择后旗标驱动色调/变体）。
    /// 注意：Test 命名空间下 Input 会解析到 PastryWorld.Input，
    /// 必须用 UnityEngine.Input 全限定（W2 同款坑）。
    /// </summary>
    public class W4RealityTestDriver : MonoBehaviour
    {
        [SerializeField] private RealityMonologueSO _deskMonologue;

        void Update()
        {
            if (UnityEngine.Input.GetKeyDown(KeyCode.F))
                RealityMonologueManager.Instance.Play(_deskMonologue);

            if (UnityEngine.Input.GetKeyDown(KeyCode.C))
            {
                Debug.Log($"[W4Driver] 当前旗标: 0x{ChoiceFlagRegistry.AllFlags:X}");
                for (int bit = 0; bit < 32; bit++)
                {
                    int flag = 1 << bit;
                    if (ChoiceFlagRegistry.Has(flag))
                        Debug.Log($"  - {ChoiceFlagRegistry.GetName(flag)}");
                }
            }
        }
    }
}
