using System;
using System.Collections.Generic;
using UnityEngine;

namespace PastryWorld.Narrative
{
    /// <summary>
    /// 对话分支选项。
    /// </summary>
    [Serializable]
    public class DialogueChoice
    {
        [Tooltip("选项文字。留空 = 沉默选项")]
        public string text;

        [Tooltip("选择后跳转的节点 ID。留空 = 结束对话")]
        public string nextNodeId;
    }

    /// <summary>
    /// 对话节点。技术预判报告 T14：
    /// speaker + text + portraitExpr + nextNodes（分支列表）
    /// + giftTrigger（可选，背包有指定点心则弹"奉上"按钮）
    /// + envTrigger（可选，进入节点时发布环境变化事件）。
    /// </summary>
    [Serializable]
    public class DialogueNode
    {
        [Header("标识")]
        [Tooltip("节点 ID（对话内唯一）")]
        public string nodeId;

        [Header("内容")]
        public string speakerName;

        [TextArea(3, 6)] public string text;

        [Tooltip("立绘。留空则不显示")]
        public Sprite portrait;

        [Tooltip("表情标签（预留 A1-4 美术表情差分索引，Demo 不使用）")]
        public string portraitExpr;

        [Header("流转")]
        [Tooltip("分支选项。为空时点击面板继续，走 nextNodeId")]
        public List<DialogueChoice> choices = new List<DialogueChoice>();

        [Tooltip("无分支时的下一节点 ID。留空 = 结束对话")]
        public string nextNodeId;

        [Header("奉上点心（可选）")]
        [Tooltip("要求背包持有的点心 ID。非空且有货时显示'奉上'按钮")]
        public string giftPastryId;

        [Tooltip("奉上后跳转的节点 ID（深层记忆对话）")]
        public string giftResponseNodeId;

        [Header("环境联动（可选）")]
        [Tooltip("进入本节点时发布 EnvironmentChangedEvent（EventBus），消费方自行响应")]
        public string envTrigger;
    }
}
