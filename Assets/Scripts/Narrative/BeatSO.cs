using UnityEngine;

namespace PastryWorld.Narrative
{
    /// <summary>
    /// 节拍类型枚举。技术预判报告 T13。
    /// </summary>
    public enum BeatType
    {
        Dialogue,
        Action,
        Memory,
        Transition,
        Choice
    }

    /// <summary>
    /// 叙事节拍 ScriptableObject 模板。
    /// </summary>
    [CreateAssetMenu(fileName = "BeatSO_", menuName = "PastryWorld/BeatSO")]
    public class BeatSO : ScriptableObject
    {
        [Header("节拍信息")]
        public string beatId;
        public BeatType beatType;

        [Header("内容")]
        [TextArea] public string dialogueText;
        public Sprite portrait;

        [Header("流转")]
        public string nextBeatId;
    }
}
