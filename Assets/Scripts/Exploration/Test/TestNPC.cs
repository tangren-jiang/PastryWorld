using UnityEngine;
using PastryWorld.Exploration;

namespace PastryWorld.Exploration.Test
{
    /// <summary>
    /// 测试用可交互 NPC。
    /// </summary>
    public class TestNPC : MonoBehaviour, IInteractable
    {
        [SerializeField] private string _npcName = "TestNPC";
        [TextArea] [SerializeField] private string _dialogue = "你好，这里是点心世界。";

        public void OnInteract()
        {
            Debug.Log($"[{_npcName}] {_dialogue}");
        }

        public string GetPromptText()
        {
            return $"按 E 与 {_npcName} 交互";
        }
    }
}
