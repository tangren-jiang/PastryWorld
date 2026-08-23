using UnityEngine;
using PastryWorld.Core;

namespace PastryWorld.Craft.Test
{
    /// <summary>
    /// T19 MiniCraft 验证驱动。
    /// F=完整制作流程 M=迷你制作流程（只跑 IsMiniStep 标记步骤）。
    /// 注意：Test 命名空间下 Input 会解析到 PastryWorld.Input，
    /// 必须用 UnityEngine.Input 全限定（W2 同款坑）。
    /// </summary>
    public class T19MiniCraftTestDriver : MonoBehaviour
    {
        [SerializeField] private CraftManager _craftManager;

        private bool _subscribed;

        void Start()
        {
            if (_craftManager != null && _craftManager.EventBus != null)
            {
                _craftManager.EventBus.Subscribe<CraftFlowCompletedEvent>(OnFlowCompleted);
                _subscribed = true;
            }
        }

        void OnDestroy()
        {
            if (_subscribed && _craftManager != null && _craftManager.EventBus != null)
                _craftManager.EventBus.Unsubscribe<CraftFlowCompletedEvent>(OnFlowCompleted);
        }

        void Update()
        {
            if (_craftManager == null) return;

            if (UnityEngine.Input.GetKeyDown(KeyCode.F))
            {
                Debug.Log("[T19Driver] 启动完整制作流程（全步骤）");
                _craftManager.StartFullCraft();
            }

            if (UnityEngine.Input.GetKeyDown(KeyCode.M))
            {
                Debug.Log("[T19Driver] 启动迷你制作流程（仅关键步骤）");
                _craftManager.StartMiniCraft();
            }
        }

        private void OnFlowCompleted(CraftFlowCompletedEvent evt)
        {
            Debug.Log($"[T19Driver] 流程结束: 总步骤={evt.totalSteps} 成功={evt.successSteps} " +
                      $"总品质={evt.overallQuality:F2} 全成功={evt.allStepsSuccess}");
        }
    }
}
