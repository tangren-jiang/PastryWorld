using UnityEngine;
using PastryWorld.Craft;
using PastryWorld.Narrative;

namespace PastryWorld.Integration.Test
{
    /// <summary>
    /// M6 系统集成测试驱动。
    /// Enter=启动 Demo 流程 / C=自动开始制作（B2 Action 节拍时）
    /// </summary>
    public class W6IntegrationTestDriver : MonoBehaviour
    {
        [SerializeField] private DemoFlowController _flowController;
        [SerializeField] CraftManager _craftManager;

        void Update()
        {
            if (UnityEngine.Input.GetKeyDown(KeyCode.Return) || UnityEngine.Input.GetKeyDown(KeyCode.Space))
            {
                _flowController.StartDemo();
            }

            if (UnityEngine.Input.GetKeyDown(KeyCode.C))
            {
                if (_craftManager != null && !_craftManager.IsRunning)
                {
                    _craftManager.StartFullCraft();
                    Debug.Log("[W6Driver] 手动启动制作流程");
                }
            }
        }
    }
}
