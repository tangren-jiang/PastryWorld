using UnityEngine;

namespace PastryWorld.Narrative
{
    /// <summary>
    /// 记忆片段播放模式。
    /// Overlay：半透明叠加（alpha 0.7），不拦截输入，制作操作继续（T6）。
    /// Fullscreen：全屏播放，锁输入，播完淡出（T15 使用，本框架预留）。
    /// </summary>
    public enum MemoryPlayMode
    {
        Overlay,
        Fullscreen
    }

    /// <summary>
    /// 记忆片段数据。帧序列 + 播放参数。
    /// 技术预判报告 T6/T15：MemoryClipPlayer 的输入，两条管线共享。
    /// 规范（A3-1）：1280×720，12fps，3-8 秒片段 = 36-96 帧。
    /// </summary>
    [CreateAssetMenu(fileName = "MemoryClip_", menuName = "PastryWorld/MemoryClip")]
    public class MemoryClipData : ScriptableObject
    {
        [Header("标识")]
        [Tooltip("记忆片段 ID，通常与 BeatSO.beatId 对应")]
        public string clipId;

        [Header("帧序列（12fps，水墨风格）")]
        [Tooltip("按播放顺序排列的帧。3 秒约 36 帧，8 秒约 96 帧")]
        public Sprite[] frames;

        [Tooltip("播放帧率。A3-1 规范：12fps 保持手绘感")]
        [Range(1, 30)] public float fps = 12f;

        [Header("播放模式")]
        [Tooltip("Overlay=半透明叠加不中断制作（T6）；Fullscreen=全屏锁输入（T15）")]
        public MemoryPlayMode playMode = MemoryPlayMode.Overlay;

        [Header("淡入淡出")]
        [Tooltip("淡入时长（秒）")]
        [Range(0f, 2f)] public float fadeInDuration = 0.3f;

        [Tooltip("淡出时长（秒）。规范：0.5s")]
        [Range(0f, 2f)] public float fadeOutDuration = 0.5f;

        [Header("叠加模式参数")]
        [Tooltip("叠加模式的目标透明度。规范：0.7")]
        [Range(0f, 1f)] public float overlayAlpha = 0.7f;

        [Header("音频")]
        [Tooltip("可选的音效/音乐片段")]
        public AudioClip audioClip;

        [Tooltip("全屏模式（T15）BGM 切换。播放期间切换到此 BGM，结束后恢复原 BGM")]
        public AudioClip bgmClip;

        /// <summary>帧序列总时长（秒）。无帧时返回 0。</summary>
        public float Duration => frames != null && frames.Length > 0 && fps > 0f
            ? frames.Length / fps
            : 0f;
    }
}
