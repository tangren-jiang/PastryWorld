using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEditor;
using UnityEditor.SceneManagement;
using PastryWorld.Exploration;
using PastryWorld.Narrative;
using PastryWorld.Reality;
using PastryWorld.Reality.Test;

namespace PastryWorld.Narrative.Editor
{
    /// <summary>
    /// W3 叙事测试场景构建脚本（T14 对话引擎 + T16 Typewriter + T15 记忆全屏 + T18 渐进色调）。
    /// 运行：Unity batchmode -executeMethod PastryWorld.Narrative.Editor.W3NarrativeTestSceneSetup.Setup
    /// 验证要点：
    /// - T14/T16：走近 NPC 按 E → 分支对话 + 逐字打字机（标点停顿）+ 奉上按钮（背包预置蜂蜜涂果干）
    /// - T13 联动：按 B → 节拍链 D1→D2，对话结束自动推进节拍
    /// - T15：按 M → 全屏记忆（移动锁定，播完解锁）
    /// - T18：按 1/2/3 → 色调过渡（中性/冷灰/暖橙）
    /// </summary>
    public static class W3NarrativeTestSceneSetup
    {
        private const string SoDir = "Assets/ScriptableObjects/Narrative";
        private const string BeatDir = "Assets/Settings/Beats";
        private const string ArtDir = "Assets/Art/UI";

        [MenuItem("Tools/PastryWorld/W3 Narrative Test Scene Setup (T14+T15+T16+T18)")]
        public static void Setup()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            EnsureFolders();

            // ---- 资产：对话/节拍/立绘 ----
            var portrait = CreatePortraitPng("Portrait_HoneyWitch", new Color(0.95f, 0.78f, 0.38f));
            var honeyDialogue = CreateHoneyWitchDialogue(portrait);
            var d1Dialogue = CreateSingleLineDialogue("Dialogue_D1", "d1", "画外音", "（巷口的桂花，开了。）");
            var d2Dialogue = CreateSingleLineDialogue("Dialogue_D2", "d2", "画外音", "（香气，把一些旧事引了出来。）");
            var beatD1 = CreateBeat("BeatSO_D1", "D1", "D2");
            var beatD2 = CreateBeat("BeatSO_D2", "D2", "");

            // ---- InputActionAsset ----
            var inputAssetGUIDs = AssetDatabase.FindAssets("PastryWorldInput t:InputActionAsset");
            InputActionAsset inputAsset = inputAssetGUIDs.Length > 0
                ? AssetDatabase.LoadAssetAtPath<InputActionAsset>(AssetDatabase.GUIDToAssetPath(inputAssetGUIDs[0]))
                : null;

            var sprite = CreateSquareSprite(64, Color.white);

            // ---- 相机 ----
            var camObj = new GameObject("MainCamera");
            var cam = camObj.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5f;
            camObj.transform.position = new Vector3(0, 0, -10);
            camObj.tag = "MainCamera";

            // ---- 玩家 ----
            var playerObj = new GameObject("Player");
            playerObj.layer = 8;
            var playerSR = playerObj.AddComponent<SpriteRenderer>();
            playerSR.sprite = sprite;
            playerSR.color = new Color(0.4f, 0.8f, 1f);
            playerSR.sortingOrder = 20;
            var playerRB = playerObj.AddComponent<Rigidbody2D>();
            playerRB.bodyType = RigidbodyType2D.Kinematic;
            var playerCol = playerObj.AddComponent<BoxCollider2D>();
            playerCol.size = new Vector2(0.8f, 0.8f);
            var playerCtrl = playerObj.AddComponent<PlayerController>();
            if (inputAsset != null)
            {
                var so = new SerializedObject(playerCtrl);
                so.FindProperty("_inputActions").objectReferenceValue = inputAsset;
                so.ApplyModifiedProperties();
            }

            // ---- NPC（E 交谈 → 对话）----
            var npcObj = new GameObject("NPC_HoneyWitch");
            npcObj.layer = 10;
            npcObj.transform.position = new Vector3(3, 0, 0);
            var npcSR = npcObj.AddComponent<SpriteRenderer>();
            npcSR.sprite = sprite;
            npcSR.color = new Color(1f, 0.8f, 0.3f);
            npcSR.sortingOrder = 20;
            var npcCol = npcObj.AddComponent<BoxCollider2D>();
            npcCol.size = new Vector2(0.8f, 0.8f);
            npcCol.isTrigger = true;
            var dialogueInteractable = npcObj.AddComponent<DialogueInteractable>();
            {
                var so = new SerializedObject(dialogueInteractable);
                so.FindProperty("_dialogue").objectReferenceValue = honeyDialogue;
                so.ApplyModifiedProperties();
            }

            // ---- 对话管理器（节拍绑定 D1/D2）----
            var dmObj = new GameObject("DialogueManager");
            var dm = dmObj.AddComponent<DialogueManager>();
            {
                var so = new SerializedObject(dm);
                var bindings = so.FindProperty("_beatBindings");
                bindings.arraySize = 2;
                bindings.GetArrayElementAtIndex(0).FindPropertyRelative("beatId").stringValue = "D1";
                bindings.GetArrayElementAtIndex(0).FindPropertyRelative("dialogue").objectReferenceValue = d1Dialogue;
                bindings.GetArrayElementAtIndex(1).FindPropertyRelative("beatId").stringValue = "D2";
                bindings.GetArrayElementAtIndex(1).FindPropertyRelative("dialogue").objectReferenceValue = d2Dialogue;
                so.ApplyModifiedProperties();
            }

            // ---- 节拍管理器 + 记忆播放器 + 色调管理器 + 测试驱动 ----
            var beatObj = new GameObject("BeatManager");
            var beatManager = beatObj.AddComponent<BeatManager>();
            {
                var so = new SerializedObject(beatManager);
                var beats = so.FindProperty("_beats");
                beats.arraySize = 2;
                beats.GetArrayElementAtIndex(0).objectReferenceValue = beatD1;
                beats.GetArrayElementAtIndex(1).objectReferenceValue = beatD2;
                so.ApplyModifiedProperties();
            }

            var memoryObj = new GameObject("MemoryPlayer");
            var memoryPlayer = memoryObj.AddComponent<MemoryClipPlayer>();

            var toneObj = new GameObject("SceneToneManager");
            var toneManager = toneObj.AddComponent<SceneToneManager>();

            var driverObj = new GameObject("W3TestDriver");
            var driver = driverObj.AddComponent<W3NarrativeTestDriver>();
            {
                var so = new SerializedObject(driver);
                so.FindProperty("_beatManager").objectReferenceValue = beatManager;
                so.FindProperty("_memoryPlayer").objectReferenceValue = memoryPlayer;
                so.FindProperty("_toneManager").objectReferenceValue = toneManager;
                so.ApplyModifiedProperties();
            }

            // ---- 地面 ----
            CreateWall("Ground", new Vector2(0, -2), new Vector2(24, 0.5f), sprite,
                new Color(0.5f, 0.4f, 0.3f), 9, 10);

            EditorSceneManager.SaveScene(scene, "Assets/Scenes/W3NarrativeTest.unity");

            Debug.Log("W3 叙事测试场景创建完成: Assets/Scenes/W3NarrativeTest.unity");
            Debug.Log("E=与婆婆交谈（分支+打字机+奉上） B=节拍链 D1→D2 M=全屏记忆 1/2/3=色调");
        }

        // ---- 资产创建 ----

        private static void EnsureFolders()
        {
            if (!AssetDatabase.IsValidFolder(SoDir))
                AssetDatabase.CreateFolder("Assets/ScriptableObjects", "Narrative");
            if (!AssetDatabase.IsValidFolder(BeatDir))
                AssetDatabase.CreateFolder("Assets/Settings", "Beats");
            if (!AssetDatabase.IsValidFolder(ArtDir))
                AssetDatabase.CreateFolder("Assets/Art", "UI");
        }

        private static DialogueSO CreateHoneyWitchDialogue(Sprite portrait)
        {
            var so = ScriptableObject.CreateInstance<DialogueSO>();
            so.dialogueId = "honey_witch_talk";
            so.startNodeId = "n1";

            so.nodes.Add(new DialogueNode
            {
                nodeId = "n1",
                speakerName = "蜂蜜婆婆",
                text = "哟，小家伙，你来啦。今天的蜂蜜涂果干，做得怎么样了？",
                portrait = portrait,
                choices =
                {
                    new DialogueChoice { text = "问问蜂蜜的讲究", nextNodeId = "n2" },
                    new DialogueChoice { text = "", nextNodeId = "n4" }, // 沉默
                }
            });

            so.nodes.Add(new DialogueNode
            {
                nodeId = "n2",
                speakerName = "蜂蜜婆婆",
                text = "蜂蜜要趁温热的时候涂。果干的酸，得让甜包着，才压得住。",
                portrait = portrait,
                nextNodeId = "n4",
                giftPastryId = "honey_fruit",
                giftResponseNodeId = "n3"
            });

            so.nodes.Add(new DialogueNode
            {
                nodeId = "n3",
                speakerName = "蜂蜜婆婆",
                text = "……这是，我记忆里的味道。谢谢你，孩子。",
                portrait = portrait,
                nextNodeId = "n4",
                envTrigger = "light_warm"
            });

            so.nodes.Add(new DialogueNode
            {
                nodeId = "n4",
                speakerName = "蜂蜜婆婆",
                text = "去吧，桂花巷的云片先生，还等着你呢。",
                portrait = portrait
            });

            AssetDatabase.CreateAsset(so, $"{SoDir}/Dialogue_HoneyWitch.asset");
            AssetDatabase.SaveAssets();
            return so;
        }

        private static DialogueSO CreateSingleLineDialogue(string fileName, string id,
            string speaker, string text)
        {
            var so = ScriptableObject.CreateInstance<DialogueSO>();
            so.dialogueId = id;
            so.startNodeId = "n1";
            so.nodes.Add(new DialogueNode { nodeId = "n1", speakerName = speaker, text = text });
            AssetDatabase.CreateAsset(so, $"{SoDir}/{fileName}.asset");
            AssetDatabase.SaveAssets();
            return so;
        }

        private static BeatSO CreateBeat(string fileName, string beatId, string nextBeatId)
        {
            var beat = ScriptableObject.CreateInstance<BeatSO>();
            beat.beatId = beatId;
            beat.beatType = BeatType.Dialogue;
            beat.nextBeatId = nextBeatId;
            AssetDatabase.CreateAsset(beat, $"{BeatDir}/{fileName}.asset");
            AssetDatabase.SaveAssets();
            return beat;
        }

        private static Sprite CreatePortraitPng(string name, Color color)
        {
            string path = $"{ArtDir}/{name}.png";
            const int size = 128;

            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color[size * size];
            float center = size * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Mathf.Sqrt((x - center) * (x - center) + (y - center) * (y - center));
                    float edge = Mathf.Clamp01((size * 0.48f - dist) / 4f);
                    pixels[y * size + x] = new Color(color.r, color.g, color.b, edge);
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());

            AssetDatabase.ImportAsset(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.SaveAndReimport();

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        // ---- 场景物件 ----

        private static void CreateWall(string name, Vector2 pos, Vector2 size, Sprite sprite,
            Color color, int layer, int sortingOrder)
        {
            var obj = new GameObject(name);
            obj.layer = layer;
            obj.transform.position = pos;
            obj.transform.localScale = size;
            var sr = obj.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = color;
            sr.sortingOrder = sortingOrder;
            var col = obj.AddComponent<BoxCollider2D>();
            col.size = Vector2.one;
        }

        private static Sprite CreateSquareSprite(int size, Color color)
        {
            var tex = new Texture2D(size, size);
            var pixels = new Color[size * size];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = color;
            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }
    }
}
