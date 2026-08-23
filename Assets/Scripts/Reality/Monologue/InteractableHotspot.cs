using UnityEngine;
using PastryWorld.Exploration;
using PastryWorld.Narrative;

namespace PastryWorld.Reality
{
    /// <summary>
    /// 现实段落定点交互热点（T17）。
    /// 一个热点二选一：播放内心独白（RealityMonologueSO）
    /// 或触发带分支的对话（DialogueSO，走 T14 DialogueManager —— 电话等选择场合）。
    /// 独白播完后可再次交互重播（repeatWhenDone）。
    /// </summary>
    public class InteractableHotspot : MonoBehaviour, IInteractable
    {
        [Header("热点名称（交互提示用）")]
        [SerializeField] private string _hotspotName = "物件";

        [Header("内容（二选一：独白优先）")]
        [SerializeField] private RealityMonologueSO _monologue;
        [SerializeField] private DialogueSO _dialogue;

        [Tooltip("独白播完后再次交互是否重播")]
        [SerializeField] private bool _repeatWhenDone = true;

        private bool _monologueDone;

        public void OnInteract()
        {
            if (_monologue != null)
            {
                if (_monologueDone && !_repeatWhenDone) return;
                RealityMonologueManager.Instance.Play(_monologue);
                _monologueDone = true;
                return;
            }

            if (_dialogue != null)
                DialogueManager.Instance.StartDialogue(_dialogue);
        }

        public string GetPromptText()
        {
            return $"查看「{_hotspotName}」";
        }
    }
}
