using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEditor;
using UnityEditor.SceneManagement;
using PastryWorld.Exploration;
using PastryWorld.Narrative;
using PastryWorld.Reality;
using PastryWorld.Reality.Test;

namespace PastryWorld.Reality.Editor
{
    /// <summary>
    /// W4 现实段落测试场景构建脚本（T17 定点交互 + T20 选择旗标系统）。
    /// 运行：Unity batchmode -executeMethod PastryWorld.Reality.Editor.W4RealityTestSceneSetup.Setup
    /// 验证要点：
    /// - T17：走近 书桌/旧照片 按 E → 多段内心独白（点击逐行推进，播完解锁）
    /// - T20：电话热点按 E → 分支对话（接起/沉默）→ 旗标置位 → 情感权重累积
    ///        → T18 色调插值 + 窗外变体三档切换（灰雨/阴天/夕阳）
    /// - 调试键：F=直接播书桌独白 C=打印当前旗标
    /// </summary>
    public static class W4RealityTestSceneSetup
    {
        private const string SoDir = "Assets/ScriptableObjects/Reality";

        [MenuItem("Tools/PastryWorld/W4 Reality Test Scene Setup (T17+T20)")]
        public static void Setup()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            EnsureFolders();

            // ---- 资产：独白 / 电话对话 ----
            var deskMonologue = CreateMonologue("Monologue_Desk", "desk_letters",
                "（摊开的稿纸，停在昨天写到一半的地方。）",
                "（那杯茶凉透了。我竟然没有察觉。）",
                "（写下去。也许写下去，就能想明白一点什么。）");
            var photoMonologue = CreateMonologue("Monologue_Photo", "photo_old",
                "（相框里的人，笑得比记忆里更年轻。）",
                "（那天也是这样的傍晚。她说，累了就歇歇。）",
                "（……好吧。就歇一会儿。）");
            var phoneDialogue = CreatePhoneDialogue();

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
            playerSR.color = new Color(0.55f, 0.6f, 0.68f);
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

            // ---- 热点：书桌（独白）----
            var desk = CreateHotspot("Hotspot_Desk", new Vector3(-3, -1.2f, 0), sprite,
                new Color(0.45f, 0.35f, 0.25f), "书桌", deskMonologue, null);
            var photo = CreateHotspot("Hotspot_Photo", new Vector3(0.5f, -0.4f, 0), sprite,
                new Color(0.5f, 0.5f, 0.55f), "旧照片", photoMonologue, null);

            // ---- 热点：电话（分支对话 → 旗标）----
            var phone = CreateHotspot("Hotspot_Phone", new Vector3(3, -1.2f, 0), sprite,
                new Color(0.3f, 0.3f, 0.35f), "旧电话", null, phoneDialogue);

            // ---- 窗外变体（T20 场景变体三档）----
            var windowCold = CreateVariant("Window_ColdRain", new Vector3(-3, 2.2f, 0), sprite,
                new Color(0.55f, 0.6f, 0.7f), 15);
            var windowNeutral = CreateVariant("Window_Cloudy", new Vector3(-3, 2.2f, 0), sprite,
                new Color(0.7f, 0.72f, 0.75f), 15);
            var windowWarm = CreateVariant("Window_Sunset", new Vector3(-3, 2.2f, 0), sprite,
                new Color(1f, 0.72f, 0.45f), 15);
            windowNeutral.SetActive(false); // 初始情感 0 → Cold 档
            windowWarm.SetActive(false);

            // ---- 管理器们 ----
            var monoObj = new GameObject("RealityMonologueManager");
            monoObj.AddComponent<RealityMonologueManager>();

            var dialogueObj = new GameObject("DialogueManager");
            dialogueObj.AddComponent<DialogueManager>();

            var toneObj = new GameObject("SceneToneManager");
            toneObj.AddComponent<SceneToneManager>();

            // ---- 选择导演（T20 规则表注入）----
            var directorObj = new GameObject("RealityChoiceDirector");
            var director = directorObj.AddComponent<RealityChoiceDirector>();
            {
                var so = new SerializedObject(director);
                var rules = so.FindProperty("_rules");
                rules.arraySize = 4;

                // 独白读完 → 旗标（温暖向）
                SetRule(rules.GetArrayElementAtIndex(0), 1, "photo_old", "", -1, 0x01, "翻看旧照片", 0.35f);
                SetRule(rules.GetArrayElementAtIndex(1), 1, "desk_letters", "", -1, 0x08, "坐回书桌", 0.25f);
                // 电话选择 → 旗标（接起=温暖，沉默=冷）
                SetRule(rules.GetArrayElementAtIndex(2), 0, "phone_call", "n1", 0, 0x02, "接起电话", 0.4f);
                SetRule(rules.GetArrayElementAtIndex(3), 0, "phone_call", "n1", 1, 0x04, "沉默挂断", -0.5f);

                var groups = so.FindProperty("_variantGroups");
                groups.arraySize = 3;
                SetGroup(groups.GetArrayElementAtIndex(0), 1, windowCold);   // Cold
                SetGroup(groups.GetArrayElementAtIndex(1), 0, windowNeutral); // Neutral
                SetGroup(groups.GetArrayElementAtIndex(2), 2, windowWarm);   // Warm

                so.ApplyModifiedProperties();
            }

            // ---- 测试驱动 ----
            var driverObj = new GameObject("W4TestDriver");
            var driver = driverObj.AddComponent<W4RealityTestDriver>();
            {
                var so = new SerializedObject(driver);
                so.FindProperty("_deskMonologue").objectReferenceValue = deskMonologue;
                so.ApplyModifiedProperties();
            }

            // ---- 地面 / 墙 ----
            CreateWall("Ground", new Vector2(0, -2), new Vector2(24, 0.5f), sprite,
                new Color(0.35f, 0.35f, 0.4f), 9, 10);

            EditorSceneManager.SaveScene(scene, "Assets/Scenes/W4RealityTest.unity");

            Debug.Log("W4 现实段落测试场景创建完成: Assets/Scenes/W4RealityTest.unity");
            Debug.Log("E=交互（书桌/照片=独白，电话=选择对话） F=直接播书桌独白 C=打印旗标；选择后观察色调与窗外变体");
        }

        // ---- 资产创建 ----

        private static void EnsureFolders()
        {
            if (!AssetDatabase.IsValidFolder("Assets/ScriptableObjects"))
                AssetDatabase.CreateFolder("Assets", "ScriptableObjects");
            if (!AssetDatabase.IsValidFolder(SoDir))
                AssetDatabase.CreateFolder("Assets/ScriptableObjects", "Reality");
        }

        private static RealityMonologueSO CreateMonologue(string fileName, string id, params string[] lines)
        {
            var so = ScriptableObject.CreateInstance<RealityMonologueSO>();
            so.monologueId = id;
            foreach (var line in lines)
                so.lines.Add(new MonologueLine { speakerName = "内心", text = line });

            AssetDatabase.CreateAsset(so, $"{SoDir}/{fileName}.asset");
            AssetDatabase.SaveAssets();
            return so;
        }

        private static DialogueSO CreatePhoneDialogue()
        {
            var so = ScriptableObject.CreateInstance<DialogueSO>();
            so.dialogueId = "phone_call";
            so.startNodeId = "n1";

            so.nodes.Add(new DialogueNode
            {
                nodeId = "n1",
                speakerName = "旧电话",
                text = "（铃——铃——铃声在空屋里显得很响。听筒拿起来，还是放下？）",
                choices =
                {
                    new DialogueChoice { text = "接起电话", nextNodeId = "n2" },
                    new DialogueChoice { text = "", nextNodeId = "n3" }, // 沉默
                }
            });
            so.nodes.Add(new DialogueNode
            {
                nodeId = "n2",
                speakerName = "听筒",
                text = "「……喂，是我。」那头的声音顿了顿，「很久没听到你做点心的声音了。」"
            });
            so.nodes.Add(new DialogueNode
            {
                nodeId = "n3",
                speakerName = "旧电话",
                text = "（铃声停了。屋子，好像比刚才更静了一点。）"
            });

            AssetDatabase.CreateAsset(so, $"{SoDir}/Dialogue_PhoneCall.asset");
            AssetDatabase.SaveAssets();
            return so;
        }

        // ---- 场景物件 ----

        private static GameObject CreateHotspot(string name, Vector3 pos, Sprite sprite, Color color,
            string hotspotName, RealityMonologueSO monologue, DialogueSO dialogue)
        {
            var obj = new GameObject(name);
            obj.layer = 10;
            obj.transform.position = pos;
            var sr = obj.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = color;
            sr.sortingOrder = 18;
            var col = obj.AddComponent<BoxCollider2D>();
            col.size = new Vector2(0.9f, 0.9f);
            col.isTrigger = true;

            var hotspot = obj.AddComponent<InteractableHotspot>();
            var so = new SerializedObject(hotspot);
            so.FindProperty("_hotspotName").stringValue = hotspotName;
            if (monologue != null) so.FindProperty("_monologue").objectReferenceValue = monologue;
            if (dialogue != null) so.FindProperty("_dialogue").objectReferenceValue = dialogue;
            so.ApplyModifiedProperties();
            return obj;
        }

        private static GameObject CreateVariant(string name, Vector3 pos, Sprite sprite, Color color, int order)
        {
            var obj = new GameObject(name);
            obj.transform.position = pos;
            obj.transform.localScale = new Vector3(3.2f, 1.8f, 1);
            var sr = obj.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = color;
            sr.sortingOrder = order;
            return obj;
        }

        private static void SetRule(SerializedProperty prop, int source, string contentId, string nodeId,
            int choiceIndex, int flag, string flagName, float weight)
        {
            prop.FindPropertyRelative("source").enumValueIndex = source;
            prop.FindPropertyRelative("contentId").stringValue = contentId;
            prop.FindPropertyRelative("nodeId").stringValue = nodeId;
            prop.FindPropertyRelative("choiceIndex").intValue = choiceIndex;
            prop.FindPropertyRelative("flag").intValue = flag;
            prop.FindPropertyRelative("flagName").stringValue = flagName;
            prop.FindPropertyRelative("emotionalWeight").floatValue = weight;
        }

        private static void SetGroup(SerializedProperty prop, int variantIndex, GameObject obj)
        {
            prop.FindPropertyRelative("variant").enumValueIndex = variantIndex;
            var objects = prop.FindPropertyRelative("objects");
            objects.arraySize = 1;
            objects.GetArrayElementAtIndex(0).objectReferenceValue = obj;
        }

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
