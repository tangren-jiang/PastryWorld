using UnityEngine;
using PastryWorld.Core;

namespace PastryWorld.Craft
{
    /// <summary>
    /// 反馈控制器。根据品质分数播放视觉/听觉反馈。
    /// 技术预判报告 T1。
    /// </summary>
    public class FeedbackController : MonoBehaviour
    {
        [Header("视觉反馈")]
        [SerializeField] protected SpriteRenderer _targetRenderer;
        [SerializeField] protected Color _successColor = new Color(0.4f, 0.9f, 0.5f);
        [SerializeField] protected Color _failColor = new Color(0.9f, 0.4f, 0.4f);
        [SerializeField] protected Color _normalColor = Color.white;

        [Header("音频反馈")]
        [SerializeField] protected AudioSource _audioSource;
        [SerializeField] protected AudioClip _successClip;
        [SerializeField] protected AudioClip _failClip;

        /// <summary>
        /// 播放品质反馈。
        /// </summary>
        /// <param name="quality">0-1 品质分数</param>
        /// <param name="success">是否通过</param>
        public virtual void PlayFeedback(float quality, bool success)
        {
            // 视觉：颜色渐变
            if (_targetRenderer != null)
            {
                _targetRenderer.color = success
                    ? Color.Lerp(_normalColor, _successColor, quality)
                    : _failColor;
            }

            // 音频
            if (_audioSource != null)
            {
                var clip = success ? _successClip : _failClip;
                if (clip != null)
                {
                    _audioSource.PlayOneShot(clip, success ? quality : 1f);
                }
            }
        }

        /// <summary>
        /// 重置视觉状态。
        /// </summary>
        public virtual void ResetFeedback()
        {
            if (_targetRenderer != null)
                _targetRenderer.color = _normalColor;
        }
    }
}
