using UnityEngine;
using PastryWorld.Exploration;

namespace PastryWorld.Narrative
{
    /// <summary>
    /// 对话型可交互物（T14）。挂在 NPC 上，玩家按交互键进入对话。
    /// 实现 Exploration 的 IInteractable（Narrative → Exploration 单向引用，
    /// DialogueManager.Instance 惰性创建，无需场景预置）。
    /// </summary>
    public class DialogueInteractable : MonoBehaviour, IInteractable
    {
        [SerializeField] private DialogueSO _dialogue;

        [SerializeField, TextArea] private string _promptText = "交谈";

        public void OnInteract()
        {
            if (_dialogue == null)
            {
                Debug.LogWarning($"[DialogueInteractable] {name} 未分配对话数据");
                return;
            }

            DialogueManager.Instance.StartDialogue(_dialogue);
        }

        public string GetPromptText() => _promptText;
    }
}
