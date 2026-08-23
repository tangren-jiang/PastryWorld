using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using PastryWorld.Codex;

namespace PastryWorld.Codex.Editor
{
    /// <summary>
    /// W5 图鉴线测试场景构建脚本（T21 数据 + T22 UI + T23 完成度）。
    /// 运行：Unity batchmode -executeMethod PastryWorld.Codex.Editor.W5CodexTestSceneSetup.Setup
    /// 验证要点：
    /// - T21：O=开关图鉴；U=解锁下一条；C=模拟制作完成（铜→银→金 品质只升不降）；
    ///        G=模拟奉上点心 → 事件驱动解锁
    /// - T22：左栏列表（分类分组+未收录？？？）+ 右栏详情翻页动画（EaseInOutQuad）
    /// - T23：顶部完成度百分比实时刷新（解锁事件驱动）
    /// </summary>
    public static class W5CodexTestSceneSetup
    {
        private const string SoDir = "Assets/ScriptableObjects/Codex";

        [MenuItem("Tools/PastryWorld/W5 Codex Test Scene Setup (T21+T22+T23)")]
        public static void Setup()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            EnsureFolders();
            var entries = CreateDemoEntries();

            // ---- 相机 ----
            var camObj = new GameObject("MainCamera");
            var cam = camObj.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5f;
            camObj.transform.position = new Vector3(0, 0, -10);
            camObj.tag = "MainCamera";

            // ---- 背景纸张色 ----
            var bg = new GameObject("Background");
            var bgSr = bg.AddComponent<SpriteRenderer>();
            bgSr.sprite = CreateSquareSprite(64, Color.white);
            bgSr.color = new Color(0.93f, 0.89f, 0.82f);
            bgSr.sortingOrder = -10;
            bg.transform.localScale = new Vector3(30, 20, 1);

            // ---- CodexManager（注入 17 条条目）----
            var mgrObj = new GameObject("CodexManager");
            var manager = mgrObj.AddComponent<CodexManager>();
            {
                var so = new SerializedObject(manager);
                var list = so.FindProperty("_entries");
                list.arraySize = entries.Count;
                for (int i = 0; i < entries.Count; i++)
                    list.GetArrayElementAtIndex(i).objectReferenceValue = entries[i];
                so.ApplyModifiedProperties();
            }

            // ---- CodexUI ----
            var uiObj = new GameObject("CodexUI");
            uiObj.AddComponent<CodexUI>();

            // ---- 测试驱动 ----
            var driverObj = new GameObject("CodexTestDriver");
            driverObj.AddComponent<CodexTestDriver>();

            EditorSceneManager.SaveScene(scene, "Assets/Scenes/W5CodexTest.unity");

            Debug.Log($"W5 图鉴测试场景创建完成: Assets/Scenes/W5CodexTest.unity（{entries.Count} 条目）");
            Debug.Log("O=开关图鉴 U=解锁下一条 C=模拟制作（品质递增） G=模拟奉上点心 R=清档");
        }

        // ---- 17 条 Demo 条目 ----

        private static List<CodexEntrySO> CreateDemoEntries()
        {
            var list = new List<CodexEntrySO>
            {
                // 地点（2）
                Entry("place_honey_garden", "蜂蜜花园", CodexCategory.Place,
                    "开满野蜜花的坡地。风一吹，空气里都是甜的。婆婆说，蜜蜂记得每一朵花开放的日子。",
                    CodexUnlockSource.Manual, "", startUnlocked: false,
                    related: new[] { "char_granny", "ing_honey" }),
                Entry("place_rain_alley", "江南烟雨巷", CodexCategory.Place,
                    "青石板路的窄巷，雨下起来就没完。巷尾的灯总亮到很晚，像在等谁回家。",
                    CodexUnlockSource.Beat, "D1",
                    related: new[] { "char_mr_cloud", "ing_rice_flour" }),

                // 点心（2）
                Entry("pastry_honey_fruit", "蜂蜜涂果干", CodexCategory.Pastry,
                    "薄薄的果干裹上一层野蜜，晒足了太阳。甜味不急，一层一层慢慢化开。",
                    CodexUnlockSource.Craft, "recipe_honey_fruit",
                    related: new[] { "ing_honey", "ing_dried_fruit" }),
                Entry("pastry_osmanthus_cake", "桂花云片糕", CodexCategory.Pastry,
                    "切得极薄的一片一片，对着光能看见桂花细细的影子。入口就散，像一口云。",
                    CodexUnlockSource.Craft, "recipe_osmanthus_cake",
                    related: new[] { "ing_osmanthus", "ing_rice_flour" }),

                // 原料（5）
                Entry("ing_honey", "野蜜", CodexCategory.Ingredient,
                    "蜂蜜婆婆亲手收的野蜜。颜色比常见的深一些，装在粗陶罐里。",
                    CodexUnlockSource.Gift, "honey_fruit",
                    related: new[] { "char_granny" }),
                Entry("ing_osmanthus", "糖桂花", CodexCategory.Ingredient,
                    "雨巷人家秋天腌下的桂花，糖渍了一个季节的香气。",
                    CodexUnlockSource.Manual, "",
                    related: new[] { "place_rain_alley" }),
                Entry("ing_dried_fruit", "果干", CodexCategory.Ingredient,
                    "太阳晒干的果子，皱皱的，甜都收在里面。", CodexUnlockSource.Manual, ""),
                Entry("ing_rice_flour", "糯米粉", CodexCategory.Ingredient,
                    "磨得极细的糯米粉，白得像初雪。云片糕绵软的秘密全在它。",
                    CodexUnlockSource.Manual, ""),
                Entry("ing_sugar_frost", "糖霜", CodexCategory.Ingredient,
                    "细细一层白霜，落在点心上，像清晨草地上的那一种。",
                    CodexUnlockSource.Manual, ""),

                // 人物（3）
                Entry("char_granny", "蜂蜜婆婆", CodexCategory.Character,
                    "守着蜂蜜花园的老人。围裙口袋里永远有一小块蜜糖，笑起来眼睛眯成一条缝。",
                    CodexUnlockSource.Manual, "",
                    related: new[] { "place_honey_garden" }),
                Entry("char_mr_cloud", "云片先生", CodexCategory.Character,
                    "雨巷尽头点心铺的主人。话不多，切云片糕的手稳得出奇。伞总是多带一把。",
                    CodexUnlockSource.Manual, "",
                    related: new[] { "place_rain_alley", "pastry_osmanthus_cake" }),
                Entry("char_lin_zhao", "林昭", CodexCategory.Character,
                    "现实里的人。在写一份好像总也写不完的稿子，抽屉里锁着一些旧物。",
                    CodexUnlockSource.Manual, ""),

                // 记忆（5）
                Entry("mem_handbook", "手账的来历", CodexCategory.Memory,
                    "这本点心手账不知道是谁放在门口的。第一页只写了一句话：把好吃的东西记下来，日子就有了形状。",
                    CodexUnlockSource.Manual, "", startUnlocked: true,
                    related: new[] { "mem_first_dream" }),
                Entry("mem_first_dream", "初次梦境", CodexCategory.Memory,
                    "第一次走进点心世界的那个梦。醒来时指尖好像还留着一点面粉的触感。",
                    CodexUnlockSource.Beat, "D1",
                    related: new[] { "mem_handbook" }),
                Entry("mem_granny_gift", "婆婆的礼物", CodexCategory.Memory,
                    "婆婆把最后一罐野蜜塞过来：「带去给会做点心的人。」",
                    CodexUnlockSource.Gift, "honey_fruit",
                    related: new[] { "char_granny", "ing_honey" }),
                Entry("mem_alley_lamp", "雨巷的灯", CodexCategory.Memory,
                    "那晚雨很大，巷尾的灯把湿青石板照出一条亮亮的路。",
                    CodexUnlockSource.Beat, "D2",
                    related: new[] { "place_rain_alley" }),
                Entry("mem_umbrella", "云片先生的伞", CodexCategory.Memory,
                    "他把伞递过来：「雨里走，点心会潮。」自己却往雨里去了。",
                    CodexUnlockSource.DialogueNode, "n3",
                    related: new[] { "char_mr_cloud" }),
            };

            foreach (var entry in list)
            {
                AssetDatabase.CreateAsset(entry, $"{SoDir}/CodexEntry_{entry.entryId}.asset");
            }
            AssetDatabase.SaveAssets();
            return list;
        }

        private static CodexEntrySO Entry(string entryId, string displayName, CodexCategory category,
            string description, CodexUnlockSource source, string sourceId,
            bool startUnlocked = false, string[] related = null)
        {
            var so = ScriptableObject.CreateInstance<CodexEntrySO>();
            so.entryId = entryId;
            so.displayName = displayName;
            so.category = category;
            so.description = description;
            so.unlockSource = source;
            so.sourceId = sourceId;
            so.startUnlocked = startUnlocked;
            so.relatedEntryIds = related ?? new string[0];
            return so;
        }

        // ---- 工具 ----

        private static void EnsureFolders()
        {
            if (!AssetDatabase.IsValidFolder("Assets/ScriptableObjects"))
                AssetDatabase.CreateFolder("Assets", "ScriptableObjects");
            if (!AssetDatabase.IsValidFolder(SoDir))
                AssetDatabase.CreateFolder("Assets/ScriptableObjects", "Codex");
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
