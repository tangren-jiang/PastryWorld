using UnityEngine;
using PastryWorld.Core;
using PastryWorld.Narrative;

namespace PastryWorld.Reality.Test
{
    /// <summary>
    /// W3 叙事测试驱动（T14/T15/T16/T18 验证）。
    /// E（InputAction）= 与 NPC 交谈（对话+打字机+奉上+分支）
    /// B = 节拍链 → 对话联动（D1→D2 自动推进）
    /// M = 全屏记忆播放（锁输入，程序化帧）
    /// 1/2/3 = 色调变体（中性/冷灰/暖橙）
    /// </summary>
    public class W3NarrativeTestDriver : MonoBehaviour
    {
        [SerializeField] private BeatManager _beatManager;
        [SerializeField] private MemoryClipPlayer _memoryPlayer;
        [SerializeField] private SceneToneManager _toneManager;

        private MemoryClipData _fullscreenClip;

        void Awake()
        {
            // 背包预置：奉上机制演示（n2 节点 giftPastryId = honey_fruit）
            SimpleInventory.Clear();
            SimpleInventory.Add("honey_fruit", "蜂蜜涂果干", 0.92f);
        }

        void Update()
        {
            // 注意：Test 命名空间下 Input 会解析到 PastryWorld.Input，必须全限定
            if (UnityEngine.Input.GetKeyDown(KeyCode.B))
            {
                Debug.Log("[W3Driver] 启动节拍链 D1（对话自动联动）");
                _beatManager.StartChain("D1");
            }

            if (UnityEngine.Input.GetKeyDown(KeyCode.M))
            {
                Debug.Log("[W3Driver] 全屏记忆播放（期间移动锁定）");
                _memoryPlayer.Play(GetFullscreenClip());
            }

            if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha1))
            {
                Debug.Log("[W3Driver] 色调 → 中性");
                _toneManager.TransitionTo(ToneVariant.Neutral);
            }

            if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha2))
            {
                Debug.Log("[W3Driver] 色调 → 冷灰现实");
                _toneManager.TransitionTo(ToneVariant.Cold);
            }

            if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha3))
            {
                Debug.Log("[W3Driver] 色调 → 暖橙梦境");
                _toneManager.TransitionTo(ToneVariant.Warm);
            }
        }

        /// <summary>程序化全屏记忆片段（36 帧 @12fps = 3 秒，水墨圆扩散）。</summary>
        private MemoryClipData GetFullscreenClip()
        {
            if (_fullscreenClip != null) return _fullscreenClip;

            const int frameCount = 36;
            var frames = new Sprite[frameCount];
            for (int i = 0; i < frameCount; i++)
                frames[i] = CreateInkFrameSprite(i / (float)(frameCount - 1));

            _fullscreenClip = ScriptableObject.CreateInstance<MemoryClipData>();
            _fullscreenClip.clipId = "runtime_fullscreen_demo";
            _fullscreenClip.frames = frames;
            _fullscreenClip.fps = 12f;
            _fullscreenClip.playMode = MemoryPlayMode.Fullscreen;
            _fullscreenClip.fadeInDuration = 0.4f;
            _fullscreenClip.fadeOutDuration = 0.6f;
            return _fullscreenClip;
        }

        private static Sprite CreateInkFrameSprite(float t)
        {
            const int size = 128;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float center = size * 0.5f;
            float radius = t * size * 0.48f;

            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Mathf.Sqrt((x - center) * (x - center) + (y - center) * (y - center));
                    float edge = Mathf.Clamp01((radius - dist) / 6f); // 柔和边缘
                    byte alpha = (byte)(edge * 200);
                    pixels[y * size + x] = new Color32(28, 24, 30, alpha); // 墨色
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }
    }
}
