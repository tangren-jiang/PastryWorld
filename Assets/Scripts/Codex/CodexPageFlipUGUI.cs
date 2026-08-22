using System.Collections;
using UnityEngine;

namespace PastryWorld.Codex
{
    /// <summary>
    /// UGUI 版图鉴翻页（Day 3 Spike 用）。
    /// 不依赖 DOTween，用协程实现 EaseInOutQuad 等价动画。
    /// </summary>
    public class CodexPageFlipUGUI : MonoBehaviour
    {
        public RectTransform pageTransform;

        /// <summary>翻页动画时长（秒），与 UI Toolkit USS transition 0.3s 对齐</summary>
        public float duration = 0.3f;

        public void FlipNext()
        {
            StartCoroutine(FlipAnimation());
        }

        IEnumerator FlipAnimation()
        {
            if (pageTransform == null) yield break;

            Vector2 startPos = pageTransform.anchoredPosition;
            Vector2 endPos = new Vector2(-Screen.width, startPos.y);
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                // EaseInOutQuad: t<0.5 ? 2t² : 1-((−2t+2)²)/2
                t = t < 0.5f ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 2f) / 2f;
                pageTransform.anchoredPosition = Vector2.Lerp(startPos, endPos, t);
                yield return null;
            }

            pageTransform.anchoredPosition = endPos;
        }
    }
}
