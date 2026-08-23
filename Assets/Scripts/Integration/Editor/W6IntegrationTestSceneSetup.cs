using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEditor;
using UnityEditor.SceneManagement;
using PastryWorld.Exploration;
using PastryWorld.Narrative;
using PastryWorld.Craft;
using PastryWorld.Reality;
using PastryWorld.Codex;
using PastryWorld.Integration;
using PastryWorld.Integration.Test;

namespace PastryWorld.Integration.Editor
{
    /// <summary>
    /// M6 系统集成测试场景构建脚本（四线合流）。
    /// 完整 Demo 流程：B1 对话→B2 制作→B3 奉上→B4 记忆→B5 收束
    /// 运行：Unity batchmode -executeMethod PastryWorld.Integration.Editor.W6IntegrationTestSceneSetup.Setup
    /// 验证：Enter=启动流程 / C=开始制作（B2 时）
    /// </summary>
    public static class W6IntegrationTestSceneSetup
    {
        private const string SoDir = "Assets/ScriptableObjects/Integration";
        private const string BeatDir = "Assets/Settings/Beats";
        private const string CraftDir = "Assets/Settings/Craft";
        private const string ArtDir = "Assets/Art/UI";

        [MenuItem("Tools/PastryWorld/W6 Integration Test Scene Setup (M6 四线合流)")]
        public static void Setup()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EnsureFolders();

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

            // ---- EventSystem ----
            var esObj = new GameObject("EventSystem");
            esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
            esObj.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();

            // ---- 资产：BeatSO B1-B5 ----
            var b1 = CreateBeat("BeatSO_B1Intro", "B1_intro", BeatType.Dialogue, "B2_craft");
            var b2 = CreateBeat("BeatSO_B2Craft", "B2_craft", BeatType.Action, "B3_gift");
            var b3 = CreateBeat("BeatSO_B3Gift", "B3_gift", BeatType.Dialogue, "B4_memory");
            var b4 = CreateBeat("BeatSO_B4Memory", "B4_memory", BeatType.Memory, "B5_ending");
            var b5 = CreateBeat("BeatSO_B5Ending", "B5_ending", BeatType.Dialogue, "");

            // ---- 资产：DialogueSO ----
            var portrait = CreatePortraitPng("Portrait_HoneyWitch", new Color(0.95f, 0.78f, 0.38f));
            var dialogueIntro = CreateIntroDialogue(portrait);
            var dialogueGift = CreateGiftDialogue(portrait);
            var dialogueEnding = CreateEndingDialogue(portrait);

            // ---- 资产：MemoryClipData（B4 记忆片段）----
            var memoryClip = CreateMemoryClip("MemoryClip_WarmMemory", "warm_memory");

            // ---- 资产：Craft StepConfigs ----
            var config1 = CreateStepConfig("SC_W6_Heat", "step_heat", "加热", 2f, 1f);
            var config2 = CreateStepConfig("SC_W6_Steam", "step_steam", "蒸制", 2f, 1f);
            var config3 = CreateStepConfig("SC_W6_Cool", "step_cool", "冷却", 2f, 1f);

            // ---- Craft 步骤（全部 CraftStep_Steaming，requireButton=false 自动完成）----
            var step1Obj = new GameObject("Step_Heat");
            var step1 = step1Obj.AddComponent<CraftStep_Steaming>();
            step1Obj.AddComponent<QualityEvaluator>();
            step1Obj.AddComponent<FeedbackController>();
            SetSteamingStep(step1, "step_heat", true, config1, 2f, 1f, 30f);

            var step2Obj = new GameObject("Step_Steam");
            var step2 = step2Obj.AddComponent<CraftStep_Steaming>();
            step2Obj.AddComponent<QualityEvaluator>();
            step2Obj.AddComponent<FeedbackController>();
            SetSteamingStep(step2, "step_steam", true, config2, 2f, 1f, 30f);

            var step3Obj = new GameObject("Step_Cool");
            var step3 = step3Obj.AddComponent<CraftStep_Steaming>();
            step3Obj.AddComponent<QualityEvaluator>();
            step3Obj.AddComponent<FeedbackController>();
            SetSteamingStep(step3, "step_cool", true, config3, 2f, 1f, 30f);

            // ---- 资产：CodexEntrySO（4 条集成验证条目）----
            var codexHoneyCake = CreateCodexEntry("CodexEntry_W6_HoneyCake", "honey_cake",
                "桂花蜂蜜糕", CodexCategory.Pastry,
                "桂花与蜂蜜交织的甜蜜。", CodexUnlockSource.Craft, "honey_cake");
            var codexGiftMemory = CreateCodexEntry("CodexEntry_W6_GiftMemory", "gift_memory_warm",
                "温暖奉上", CodexCategory.Memory,
                "奉上点心的那一刻，旧日的温度回来了。", CodexUnlockSource.Gift, "honey_cake");
            var codexBeat = CreateCodexEntry("CodexEntry_W6_Beat", "beat_b4_memory",
                "记忆节拍", CodexCategory.Memory,
                "B4 记忆节拍触发。", CodexUnlockSource.Beat, "B4_memory");
            var codexNode = CreateCodexEntry("CodexEntry_W6_Node", "node_n2_gift",
                "蜂蜜婆婆的回应", CodexCategory.Character,
                "她尝到了那味道，眼睛亮了一下。", CodexUnlockSource.DialogueNode, "n2_gift");

            // ---- 管理器 ----

            // BeatManager
            var beatMgrObj = new GameObject("BeatManager");
            var beatMgr = beatMgrObj.AddComponent<BeatManager>();
            {
                var so = new SerializedObject(beatMgr);
                var beats = so.FindProperty("_beats");
                beats.arraySize = 5;
                beats.GetArrayElementAtIndex(0).objectReferenceValue = b1;
                beats.GetArrayElementAtIndex(1).objectReferenceValue = b2;
                beats.GetArrayElementAtIndex(2).objectReferenceValue = b3;
                beats.GetArrayElementAtIndex(3).objectReferenceValue = b4;
                beats.GetArrayElementAtIndex(4).objectReferenceValue = b5;
                so.FindProperty("_startBeatId").stringValue = "B1_intro";
                so.ApplyModifiedProperties();
            }

            // DialogueManager（beat 绑定 B1→Intro, B3→Gift, B5→Ending）
            var dialogueMgrObj = new GameObject("DialogueManager");
            var dialogueMgr = dialogueMgrObj.AddComponent<DialogueManager>();
            {
                var so = new SerializedObject(dialogueMgr);
                var bindings = so.FindProperty("_beatBindings");
                bindings.arraySize = 3;
                // B1 → Intro
                var b0 = bindings.GetArrayElementAtIndex(0);
                b0.FindPropertyRelative("beatId").stringValue = "B1_intro";
                b0.FindPropertyRelative("dialogue").objectReferenceValue = dialogueIntro;
                // B3 → Gift
                var b1g = bindings.GetArrayElementAtIndex(1);
                b1g.FindPropertyRelative("beatId").stringValue = "B3_gift";
                b1g.FindPropertyRelative("dialogue").objectReferenceValue = dialogueGift;
                // B5 → Ending
                var b2g = bindings.GetArrayElementAtIndex(2);
                b2g.FindPropertyRelative("beatId").stringValue = "B5_ending";
                b2g.FindPropertyRelative("dialogue").objectReferenceValue = dialogueEnding;
                so.ApplyModifiedProperties();
            }

            // CraftManager
            var craftMgrObj = new GameObject("CraftManager");
            craftMgrObj.AddComponent<AdaptiveDifficulty>();
            var craftMgr = craftMgrObj.AddComponent<CraftManager>();
            {
                var so = new SerializedObject(craftMgr);
                var steps = so.FindProperty("_steps");
                steps.arraySize = 3;
                steps.GetArrayElementAtIndex(0).objectReferenceValue = step1;
                steps.GetArrayElementAtIndex(1).objectReferenceValue = step2;
                steps.GetArrayElementAtIndex(2).objectReferenceValue = step3;
                so.FindProperty("_autoStart").boolValue = false;
                so.FindProperty("_miniCraftMode").boolValue = false;
                so.FindProperty("_recipeId").stringValue = "honey_cake";
                so.ApplyModifiedProperties();
            }

            // MemoryClipPlayer
            var memObj = new GameObject("MemoryClipPlayer");
            var memPlayer = memObj.AddComponent<MemoryClipPlayer>();

            // SceneToneManager
            var toneObj = new GameObject("SceneToneManager");
            toneObj.AddComponent<SceneToneManager>();

            // RealityChoiceDirector（选择规则表）
            var directorObj = new GameObject("RealityChoiceDirector");
            var director = directorObj.AddComponent<RealityChoiceDirector>();
            {
                var so = new SerializedObject(director);
                var rules = so.FindProperty("_rules");
                rules.arraySize = 2;
                // 规则1：对话 intro 节点 n1 选择 0（"好的"）→ 旗标 0x01 温暖 +0.4
                var r0 = rules.GetArrayElementAtIndex(0);
                r0.FindPropertyRelative("source").enumValueIndex = 0; // DialogueChoice
                r0.FindPropertyRelative("contentId").stringValue = "intro";
                r0.FindPropertyRelative("nodeId").stringValue = "n1_intro";
                r0.FindPropertyRelative("choiceIndex").intValue = 0;
                r0.FindPropertyRelative("flag").intValue = 0x01;
                r0.FindPropertyRelative("flagName").stringValue = "答应做糕";
                r0.FindPropertyRelative("emotionalWeight").floatValue = 0.4f;
                // 规则2：对话 intro 节点 n1 选择 1（"……"）→ 旗标 0x02 冷淡 -0.2
                var r1 = rules.GetArrayElementAtIndex(1);
                r1.FindPropertyRelative("source").enumValueIndex = 0;
                r1.FindPropertyRelative("contentId").stringValue = "intro";
                r1.FindPropertyRelative("nodeId").stringValue = "n1_intro";
                r1.FindPropertyRelative("choiceIndex").intValue = 1;
                r1.FindPropertyRelative("flag").intValue = 0x02;
                r1.FindPropertyRelative("flagName").stringValue = "沉默";
                r1.FindPropertyRelative("emotionalWeight").floatValue = -0.2f;
                so.ApplyModifiedProperties();
            }

            // CodexManager
            var codexMgrObj = new GameObject("CodexManager");
            var codexMgr = codexMgrObj.AddComponent<CodexManager>();
            {
                var so = new SerializedObject(codexMgr);
                var entries = so.FindProperty("_entries");
                entries.arraySize = 4;
                entries.GetArrayElementAtIndex(0).objectReferenceValue = codexHoneyCake;
                entries.GetArrayElementAtIndex(1).objectReferenceValue = codexGiftMemory;
                entries.GetArrayElementAtIndex(2).objectReferenceValue = codexBeat;
                entries.GetArrayElementAtIndex(3).objectReferenceValue = codexNode;
                so.ApplyModifiedProperties();
            }

            // DemoFlowController
            var flowObj = new GameObject("DemoFlowController");
            var flowCtrl = flowObj.AddComponent<DemoFlowController>();
            {
                var so = new SerializedObject(flowCtrl);
                so.FindProperty("_beatManager").objectReferenceValue = beatMgr;
                so.FindProperty("_craftManager").objectReferenceValue = craftMgr;
                so.FindProperty("_memoryPlayer").objectReferenceValue = memPlayer;
                var memBindings = so.FindProperty("_memoryBindings");
                memBindings.arraySize = 1;
                var mb0 = memBindings.GetArrayElementAtIndex(0);
                mb0.FindPropertyRelative("beatId").stringValue = "B4_memory";
                mb0.FindPropertyRelative("clip").objectReferenceValue = memoryClip;
                so.FindProperty("_craftToPastryId").stringValue = "honey_cake";
                so.FindProperty("_craftToDisplayName").stringValue = "桂花蜂蜜糕";
                so.ApplyModifiedProperties();
            }

            // 测试驱动
            var driverObj = new GameObject("W6TestDriver");
            var driver = driverObj.AddComponent<W6IntegrationTestDriver>();
            {
                var so = new SerializedObject(driver);
                so.FindProperty("_flowController").objectReferenceValue = flowCtrl;
                so.FindProperty("_craftManager").objectReferenceValue = craftMgr;
                so.ApplyModifiedProperties();
            }

            // 场景保存
            var scenePath = "Assets/Scenes/W6IntegrationTest.unity";
            EditorSceneManager.SaveScene(scene, scenePath);
            AssetDatabase.SaveAssets();

            Debug.Log("M6 系统集成测试场景创建完成: " + scenePath);
            Debug.Log("Enter=启动 Demo 流程 / C=开始制作（B2 Action 节拍时）");
            Debug.Log("流程：B1 对话(分支)→B2 制作→B3 奉上→B4 记忆→B5 收束");
        }

        // ==================== 资产创建辅助 ====================

        private static void EnsureFolders()
        {
            if (!AssetDatabase.IsValidFolder("Assets/ScriptableObjects/Integration"))
                AssetDatabase.CreateFolder("Assets/ScriptableObjects", "Integration");
            if (!AssetDatabase.IsValidFolder("Assets/Settings/Beats"))
            {
                if (!AssetDatabase.IsValidFolder("Assets/Settings"))
                    AssetDatabase.CreateFolder("Assets", "Settings");
                AssetDatabase.CreateFolder("Assets/Settings", "Beats");
            }
            if (!AssetDatabase.IsValidFolder("Assets/Settings/Craft"))
                AssetDatabase.CreateFolder("Assets/Settings", "Craft");
            if (!AssetDatabase.IsValidFolder("Assets/Art/UI"))
            {
                if (!AssetDatabase.IsValidFolder("Assets/Art"))
                    AssetDatabase.CreateFolder("Assets", "Art");
                AssetDatabase.CreateFolder("Assets/Art", "UI");
            }
            if (!AssetDatabase.IsValidFolder("Assets/ScriptableObjects/Codex"))
                AssetDatabase.CreateFolder("Assets/ScriptableObjects", "Codex");
        }

        private static BeatSO CreateBeat(string fileName, string beatId, BeatType beatType, string nextBeatId)
        {
            var beat = ScriptableObject.CreateInstance<BeatSO>();
            beat.beatId = beatId;
            beat.beatType = beatType;
            beat.dialogueText = beatId;
            beat.nextBeatId = nextBeatId;
            AssetDatabase.CreateAsset(beat, $"{BeatDir}/{fileName}.asset");
            return beat;
        }

        private static DialogueSO CreateIntroDialogue(Sprite portrait)
        {
            var dlg = ScriptableObject.CreateInstance<DialogueSO>();
            dlg.dialogueId = "intro";
            dlg.startNodeId = "n1_intro";
            var n1 = new DialogueNode
            {
                nodeId = "n1_intro",
                speakerName = "蜂蜜婆婆",
                text = "你来了。能帮我做一份桂花蜂蜜糕吗？",
                portrait = portrait
            };
            n1.choices.Add(new DialogueChoice { text = "好的，我去做", nextNodeId = "" });
            n1.choices.Add(new DialogueChoice { text = "", nextNodeId = "" }); // 沉默选项
            dlg.nodes.Add(n1);
            AssetDatabase.CreateAsset(dlg, $"{SoDir}/Dialogue_Intro.asset");
            return dlg;
        }

        private static DialogueSO CreateGiftDialogue(Sprite portrait)
        {
            var dlg = ScriptableObject.CreateInstance<DialogueSO>();
            dlg.dialogueId = "gift";
            dlg.startNodeId = "n1_gift";
            var n1 = new DialogueNode
            {
                nodeId = "n1_gift",
                speakerName = "蜂蜜婆婆",
                text = "你做好了？让我尝尝。",
                portrait = portrait,
                giftPastryId = "honey_cake",
                giftResponseNodeId = "n2_gift"
            };
            var n2 = new DialogueNode
            {
                nodeId = "n2_gift",
                speakerName = "蜂蜜婆婆",
                text = "嗯……真好吃。这味道，让我想起了一些旧事。",
                portrait = portrait,
                envTrigger = "light_warm",
                nextNodeId = "" // 结束对话
            };
            dlg.nodes.Add(n1);
            dlg.nodes.Add(n2);
            AssetDatabase.CreateAsset(dlg, $"{SoDir}/Dialogue_Gift.asset");
            return dlg;
        }

        private static DialogueSO CreateEndingDialogue(Sprite portrait)
        {
            var dlg = ScriptableObject.CreateInstance<DialogueSO>();
            dlg.dialogueId = "ending";
            dlg.startNodeId = "n1_ending";
            var n1 = new DialogueNode
            {
                nodeId = "n1_ending",
                speakerName = "蜂蜜婆婆",
                text = "谢谢你，孩子。这份心意，我收下了。",
                portrait = portrait,
                nextNodeId = ""
            };
            dlg.nodes.Add(n1);
            AssetDatabase.CreateAsset(dlg, $"{SoDir}/Dialogue_Ending.asset");
            return dlg;
        }

        private static MemoryClipData CreateMemoryClip(string fileName, string clipId)
        {
            var clip = ScriptableObject.CreateInstance<MemoryClipData>();
            clip.clipId = clipId;
            clip.fps = 12f;
            clip.playMode = MemoryPlayMode.Fullscreen;
            clip.fadeInDuration = 0.3f;
            clip.fadeOutDuration = 0.5f;

            // 生成 36 帧水墨圆扩散（128×128）
            var frames = new Sprite[36];
            for (int i = 0; i < 36; i++)
            {
                float t = i / 35f;
                frames[i] = CreateCircleSprite(128, t);
            }
            clip.frames = frames;

            AssetDatabase.CreateAsset(clip, $"{SoDir}/{fileName}.asset");
            // 保存帧纹理为子资产
            foreach (var frame in frames)
            {
                if (frame != null && frame.texture != null)
                    AssetDatabase.AddObjectToAsset(frame.texture, clip);
            }
            AssetDatabase.SaveAssets();
            return clip;
        }

        private static StepConfig CreateStepConfig(string fileName, string stepId, string displayName,
            float target, float tolerance)
        {
            var config = ScriptableObject.CreateInstance<StepConfig>();
            config.stepId = stepId;
            config.displayName = displayName;
            config.description = $"W6 集成: {displayName}";
            config.targetValue = target;
            config.tolerance = tolerance;
            config.passThreshold = 0.5f;
            config.qualityWeight = 1f;
            AssetDatabase.CreateAsset(config, $"{CraftDir}/{fileName}.asset");
            return config;
        }

        private static void SetSteamingStep(CraftStep_Steaming step, string stepId, bool isMini,
            StepConfig config, float optimalTime, float toleranceTime, float maxTime)
        {
            var so = new SerializedObject(step);
            so.FindProperty("_stepId").stringValue = stepId;
            so.FindProperty("_isMiniStep").boolValue = isMini;
            so.FindProperty("_config").objectReferenceValue = config;
            so.FindProperty("_inputType").enumValueIndex = (int)CraftInputType.Timing;
            so.FindProperty("_optimalTime").floatValue = optimalTime;
            so.FindProperty("_toleranceTime").floatValue = toleranceTime;
            so.FindProperty("_maxTime").floatValue = maxTime;
            so.FindProperty("_requireButton").boolValue = false;
            so.ApplyModifiedProperties();
        }

        private static CodexEntrySO CreateCodexEntry(string fileName, string entryId,
            string displayName, CodexCategory category, string description,
            CodexUnlockSource source, string sourceId)
        {
            var entry = ScriptableObject.CreateInstance<CodexEntrySO>();
            entry.entryId = entryId;
            entry.displayName = displayName;
            entry.category = category;
            entry.description = description;
            entry.unlockSource = source;
            entry.sourceId = sourceId;
            entry.startUnlocked = false;
            AssetDatabase.CreateAsset(entry, $"Assets/ScriptableObjects/Codex/{fileName}.asset");
            return entry;
        }

        // ---- 程序化精灵 ----

        private static Sprite CreateSquareSprite(int size, Color color)
        {
            var tex = new Texture2D(size, size);
            var pixels = new Color[size * size];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = color;
            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        private static Sprite CreateCircleSprite(int size, float progress)
        {
            var tex = new Texture2D(size, size);
            var pixels = new Color[size * size];
            float cx = size * 0.5f;
            float cy = size * 0.5f;
            float maxR = size * 0.45f;
            float r = maxR * Mathf.Clamp01(progress);
            float inkAlpha = Mathf.Lerp(0.9f, 0.2f, progress);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy));
                    int idx = y * size + x;
                    if (dist <= r)
                        pixels[idx] = new Color(0.15f, 0.12f, 0.1f, inkAlpha);
                    else
                        pixels[idx] = new Color(0, 0, 0, 0);
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        private static Sprite CreatePortraitPng(string fileName, Color tint)
        {
            var size = 128;
            var tex = new Texture2D(size, size);
            var pixels = new Color[size * size];
            float cx = size * 0.5f;
            float cy = size * 0.5f;
            float r = size * 0.4f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy));
                    int idx = y * size + x;
                    if (dist <= r)
                        pixels[idx] = tint;
                    else
                        pixels[idx] = new Color(0, 0, 0, 0);
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();

            var path = $"{ArtDir}/{fileName}.png";
            File.WriteAllBytes(path, tex.EncodeToPNG());
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
    }
}
