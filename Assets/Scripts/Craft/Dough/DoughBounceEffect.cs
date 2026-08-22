using System.Collections;
using UnityEngine;

namespace PastryWorld.Craft.Dough
{
    /// <summary>
    /// 蒸糯弹性回弹效果。蒸制完成后 scale 1.0→1.05→1.0。
    /// 技术预判报告 A4：蒸糯会弹。
    /// </summary>
    public class DoughBounceEffect : MonoBehaviour
    {
        [Header("配置")]
        [SerializeField] private DoughPhysicsConfig _config;

        /// <summary>
        /// 播放弹性回弹动画。
        /// </summary>
        public void PlayBounce()
        {
            float scale = _config != null ? _config.bounceScale : 1.05f;
            float duration = _config != null ? _config.bounceDuration : 0.3f;
            StartCoroutine(BounceRoutine(scale, duration));
        }

        private IEnumerator BounceRoutine(float peakScale, float duration)
        {
            Vector3 originalScale = transform.localScale;
            Vector3 peakScaleVec = originalScale * peakScale;

            // 弹出（0→peak，前半段）
            float t = 0f;
            while (t < duration * 0.4f)
            {
                t += Time.deltaTime;
                float progress = t / (duration * 0.4f);
                // EaseOutQuad
                progress = 1f - (1f - progress) * (1f - progress);
                transform.localScale = Vector3.Lerp(originalScale, peakScaleVec, progress);
                yield return null;
            }

            // 回弹（peak→original，后半段）
            t = 0f;
            while (t < duration * 0.6f)
            {
                t += Time.deltaTime;
                float progress = t / (duration * 0.6f);
                // EaseOutBounce 近似
                progress = 1f - Mathf.Pow(1f - progress, 3f);
                transform.localScale = Vector3.Lerp(peakScaleVec, originalScale, progress);
                yield return null;
            }

            transform.localScale = originalScale;
        }
    }
}
