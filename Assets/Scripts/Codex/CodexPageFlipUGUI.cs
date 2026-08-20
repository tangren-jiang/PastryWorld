// Day 3 Spike 用：导入 DOTween 后在 Player Settings 添加 DOTWEEN_ENABLED 宏启用
#if DOTWEEN_ENABLED
using UnityEngine;
using DG.Tweening;

namespace PastryWorld.Codex
{
    /// <summary>
    /// UGUI 版图鉴翻页（Day 3 Spike 用）。
    /// </summary>
    public class CodexPageFlipUGUI : MonoBehaviour
    {
        public RectTransform pageTransform;

        public void FlipNext()
        {
            pageTransform.DOAnchorPosX(
                -Screen.width, 0.3f).SetEase(Ease.InOutQuad);
        }
    }
}
#endif
