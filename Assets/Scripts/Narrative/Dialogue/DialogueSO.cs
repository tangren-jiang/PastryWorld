using System.Collections.Generic;
using UnityEngine;

namespace PastryWorld.Narrative
{
    /// <summary>
    /// 对话数据（T14）。节点列表 + 入口节点。
    /// K1-K5 约 30-40 个节点的配置容器。
    /// </summary>
    [CreateAssetMenu(fileName = "Dialogue_", menuName = "PastryWorld/Dialogue")]
    public class DialogueSO : ScriptableObject
    {
        [Header("标识")]
        [Tooltip("对话 ID。通常与 BeatSO.beatId 或 NPC 对应")]
        public string dialogueId;

        [Tooltip("入口节点 ID")]
        public string startNodeId;

        [Header("节点列表")]
        public List<DialogueNode> nodes = new List<DialogueNode>();

        public DialogueNode StartNode => GetNode(startNodeId);

        public DialogueNode GetNode(string nodeId)
        {
            if (string.IsNullOrEmpty(nodeId)) return null;
            return nodes.Find(n => n.nodeId == nodeId);
        }
    }
}
