using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using PastryWorld.Narrative;

namespace PastryWorld.Narrative.Editor
{
    /// <summary>
    /// T6 记忆叠加测试场景构建脚本。
    /// 运行：Unity batchmode -executeMethod PastryWorld.Narrative.Editor.T6MemoryTestSceneSetup.Setup
    /// 或菜单 Tools/PastryWorld/T6 Memory Test Scene Setup
    ///
    /// 验证点：
    /// 1. 半透明叠加播放（alpha 0.7，12fps 帧序列）
    /// 2. 叠加期间制作输入不中断（可拖拽面团方块）
    /// 3. 播完后 0.5s 淡出
    /// 4. BeatManager 联动（Memory 节拍自动触发）
    /// </summary>
    public static class T6MemoryTestSceneSetup
    {
        private const int FrameCount = 36;   // 3 秒 × 12fps
        private const int FrameSize = 256;

        [MenuItem("Tools/PastryWorld/T6 Memory Test Scene Setup")]
        public static void Setup()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 主相机
            var camObj = new GameObject("MainCamera");
            var cam = camObj.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5f;
            cam.backgroundColor = new Color(0.15f, 0.14f, 0.13f);
            camObj.transform.position = new Vector3(0, 0, -10);
            camObj.tag = "MainCamera";

            // 1. 生成占位帧序列（水墨圆扩散动画）
            var frames = GeneratePlaceholderFrames();

            // 2. 记忆片段 SO
            var clip = ScriptableObject.CreateInstance<MemoryClipData>();
            clip.clipId = "K3";
            clip.frames = frames;
            clip.fps = 12f;
            clip.playMode = MemoryPlayMode.Overlay;
            clip.fadeInDuration = 0.3f;
            clip.fadeOutDuration = 0.5f;
            clip.overlayAlpha = 0.7f;

            // 3. 制作层占位（可拖拽的面团方块，验证叠加不拦截输入）
            var doughObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            doughObj.name = "DoughBlock_CanDragDuringOverlay";
            doughObj.transform.position = new Vector3(0, -2, 0);
            doughObj.transform.localScale = new Vector3(1.5f, 1.5f, 1f);
            doughObj.GetComponent<Renderer>().material.color = new Color(0.85f, 0.78f, 0.65f);
            doughObj.AddComponent<CraftSimulator>();

            // 4. BeatManager + 节拍链：K1(对话) → K3(记忆叠加) → K4(对话)
            var beats = new BeatSO[3];
            string[] ids = { "K1", "K3", "K4" };
            string[] texts = {
                "蜂蜜婆婆：这蜂蜜，是我年轻时采的花蜜。",
                "【记忆闪回——金色花海】",
                "蜂蜜婆婆：那时候，一切都还是甜的。"
            };
            BeatType[] types = { BeatType.Dialogue, BeatType.Memory, BeatType.Dialogue };

            for (int i = 0; i < 3; i++)
            {
                beats[i] = ScriptableObject.CreateInstance<BeatSO>();
                beats[i].beatId = ids[i];
                beats[i].beatType = types[i];
                beats[i].dialogueText = texts[i];
                beats[i].nextBeatId = i < 2 ? ids[i + 1] : "";
            }

            var beatMgrObj = new GameObject("BeatManager");
            var beatMgr = beatMgrObj.AddComponent<BeatManager>();
            var so = new SerializedObject(beatMgr);
            so.FindProperty("_beats").arraySize = 3;
            for (int i = 0; i < 3; i++)
            {
                so.FindProperty("_beats").GetArrayElementAtIndex(i).objectReferenceValue = beats[i];
            }
            so.FindProperty("_startBeatId").stringValue = "K1";
            so.ApplyModifiedProperties();

            // 5. MemoryClipPlayer + MemoryOverlayController
            var memoryObj = new GameObject("MemorySystem");
            var player = memoryObj.AddComponent<MemoryClipPlayer>();
            var overlay = memoryObj.AddComponent<MemoryOverlayController>();

            var soOverlay = new SerializedObject(overlay);
            soOverlay.FindProperty("_player").objectReferenceValue = player;
            soOverlay.FindProperty("_beatManager").objectReferenceValue = beatMgr;
            soOverlay.FindProperty("_clips").arraySize = 1;
            soOverlay.FindProperty("_clips").GetArrayElementAtIndex(0).objectReferenceValue = clip;
            soOverlay.ApplyModifiedProperties();

            // 6. 测试驱动器
            var driverObj = new GameObject("T6TestDriver");
            driverObj.AddComponent<T6TestDriver>();

            // 保存资产
            if (!AssetDatabase.IsValidFolder("Assets/Settings/Memory"))
                AssetDatabase.CreateFolder("Assets/Settings", "Memory");
            if (!AssetDatabase.IsValidFolder("Assets/Settings/Beats"))
                AssetDatabase.CreateFolder("Assets/Settings", "Beats");

            for (int i = 0; i < 3; i++)
            {
                string path = $"Assets/Settings/Beats/BeatSO_T6_{ids[i]}.asset";
                AssetDatabase.CreateAsset(beats[i], path);
            }
            AssetDatabase.CreateAsset(clip, "Assets/Settings/Memory/MemoryClip_K3.asset");

            if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
                AssetDatabase.CreateFolder("Assets", "Scenes");
            string scenePath = "Assets/Scenes/T6MemoryTest.unity";
            EditorSceneManager.SaveScene(scene, scenePath);
            AssetDatabase.SaveAssets();

            Debug.Log($"T6 记忆叠加测试场景创建完成: {scenePath}");
            Debug.Log($"占位帧 {FrameCount} 张已生成（水墨圆扩散，12fps × 3s）");
            Debug.Log("验证：进入 Play 模式后按空格推进节拍到 K3，叠加播放时拖拽面团方块应不受影响");
        }

        /// <summary>
        /// 生成占位水墨帧序列：圆从中心扩散并淡出，模拟记忆闪回。
        /// </summary>
        private static Sprite[] GeneratePlaceholderFrames()
        {
            string dir = "Assets/Art/MemoryTestFrames";
            if (!AssetDatabase.IsValidFolder("Assets/Art"))
                AssetDatabase.CreateFolder("Assets", "Art");
            if (!AssetDatabase.IsValidFolder(dir))
                AssetDatabase.CreateFolder("Assets/Art", "MemoryTestFrames");

            var frames = new Sprite[FrameCount];

            for (int i = 0; i < FrameCount; i++)
            {
                float t = i / (float)(FrameCount - 1);
                var tex = new Texture2D(FrameSize, FrameSize, TextureFormat.RGBA32, false);
                float radius = Mathf.Lerp(0.15f, 0.45f, t) * FrameSize;
                float alpha = Mathf.Lerp(1f, 0.2f, t);
                var center = new Vector2(FrameSize * 0.5f, FrameSize * 0.5f);

                for (int y = 0; y < FrameSize; y++)
                {
                    for (int x = 0; x < FrameSize; x++)
                    {
                        float dist = Vector2.Distance(new Vector2(x, y), center);
                        // 边缘羽化（模拟水墨晕染）
                        float edge = Mathf.Clamp01((radius - dist) / (radius * 0.3f));
                        // 内部透明（空心水墨圈）
                        float hollow = Mathf.Clamp01((dist - radius * 0.5f) / (radius * 0.3f));
                        float a = alpha * edge * hollow;
                        tex.SetPixel(x, y, new Color(0.92f, 0.9f, 0.82f, a));
                    }
                }
                tex.Apply();

                string path = $"{dir}/frame_{i:D3}.png";
                File.WriteAllBytes(path, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
            }

            AssetDatabase.Refresh();

            for (int i = 0; i < FrameCount; i++)
            {
                string path = $"{dir}/frame_{i:D3}.png";
                var importer = (UnityEditor.TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType = UnityEditor.TextureImporterType.Sprite;
                importer.spriteImportMode = UnityEditor.SpriteImportMode.Single;
                importer.SaveAndReimport();
                frames[i] = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            }

            return frames;
        }
    }

    /// <summary>
    /// 测试驱动器。按空格推进节拍，验证 K3 记忆叠加触发。
    /// </summary>
    public class T6TestDriver : MonoBehaviour
    {
        private BeatManager _beatMgr;
        private MemoryClipPlayer _player;

        void Start()
        {
            _beatMgr = FindObjectOfType<BeatManager>();
            _player = FindObjectOfType<MemoryClipPlayer>();

            if (_beatMgr != null)
            {
                _beatMgr.StartChain();
                Debug.Log($"[T6] 节拍链启动: {_beatMgr.CurrentBeatId}。按空格推进到 K3 触发记忆叠加");
            }
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                if (_beatMgr != null && _beatMgr.IsRunning)
                {
                    _beatMgr.Advance();
                    Debug.Log($"[T6] 推进到: {_beatMgr.CurrentBeatId ?? "(链结束)"} | 播放中: {(_player != null && _player.IsPlaying)}");
                }
            }
        }
    }

    /// <summary>
    /// 制作模拟器。按住鼠标拖拽方块，验证叠加层不拦截输入。
    /// </summary>
    public class CraftSimulator : MonoBehaviour
    {
        private bool _dragging;
        private Vector3 _offset;

        void OnMouseDown()
        {
            _dragging = true;
            _offset = transform.position - Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Debug.Log("[T6] 拖拽开始（若记忆叠加中仍可拖拽 = blocksRaycasts=false 生效）");
        }

        void OnMouseUp()
        {
            _dragging = false;
        }

        void Update()
        {
            if (_dragging)
            {
                Vector3 target = Camera.main.ScreenToWorldPoint(Input.mousePosition) + _offset;
                target.z = 0f;
                transform.position = target;
            }
        }
    }
}
