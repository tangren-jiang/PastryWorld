using UnityEngine;
using UnityEngine.UIElements;

namespace PastryWorld.Codex
{
    /// <summary>
    /// UI Toolkit 版图鉴翻页（Day 3 Spike 用）。
    /// </summary>
    public class CodexPageFlipUITK : MonoBehaviour
    {
        private VisualElement _root;
        private VisualElement _entry;

        void Start()
        {
            _root = GetComponent<UIDocument>().rootVisualElement;
            _entry = _root.Q<VisualElement>(className: "codex-entry");
        }

        public void FlipNext()
        {
            _entry.AddToClassList("flip-out");
        }
    }
}
