using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using PastryWorld.Craft;
using PastryWorld.Input;

namespace PastryWorld.Craft.Editor
{
    /// <summary>
    /// T19 MiniCraft 验证场景构建脚本。
    /// 三步骤配方：称量（非 mini）→ 揉面（mini）→ 蒸制（mini）。
    /// 迷你模式只跑 揉面+蒸制，完整模式跑全部三步。
    /// 运行：Unity batchmode -executeMethod PastryWorld.Craft.Editor.T19MiniCraftTestSceneSetup.Setup
    /// 验证：F=完整流程（3 步） M=迷你流程（2 步，跳过称量）
    /// </summary>
    public static class T19MiniCraftTestSceneSetup
    {
        [MenuItem("Tools/PastryWorld/T19 MiniCraft Test Scene Setup")]
        public static void Setup()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            EnsureFolders();

            // ---- 相机 ----
            var camObj = new GameObject("MainCamera");
            var cam = camObj.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5f;
            camObj.transform.position = new Vector3(0, 0, -10);
            camObj.tag = "MainCamera";

            var sprite = CreateSquareSprite(32, Color.white);

            // ---- 输入提供者 ----
            var inputObj = new GameObject("MouseInput");
            var inputProvider = inputObj.AddComponent<MouseInputProvider>();

            // ---- 步骤配置 ----
            var weighConfig = CreateConfig("StepConfig_Mini_Weigh", "weigh_honey", "称量蜂蜜",
                100f, 15f, 0.6f, 1f);
            var kneadConfig = CreateConfig("StepConfig_Mini_Knead", "knead_dough", "揉面",
                3f, 1f, 0.6f, 1f);
            var steamConfig = CreateConfig("StepConfig_Mini_Steam", "steam_pastry", "蒸制",
                5f, 1.5f, 0.6f, 1f);

            // ---- 步骤 1：称量（非 mini——迷你模式应跳过）----
            var weighObj = new GameObject("Step_Weighing");
            var weighStep = weighObj.AddComponent<CraftStep_Weighing>();
            weighObj.AddComponent<QualityEvaluator>();
            weighObj.AddComponent<FeedbackController>();
            {
                var so = new SerializedObject(weighStep);
                so.FindProperty("_stepId").stringValue = "weigh_honey";
                so.FindProperty("_isMiniStep").boolValue = false;
                so.FindProperty("_config").objectReferenceValue = weighConfig;
                so.FindProperty("_inputType").enumValueIndex = (int)CraftInputType.Drag;
                so.FindProperty("_pourRate").floatValue = 50f;
                so.FindProperty("_maxWeight").floatValue = 200f;
                so.FindProperty("_inputProviderObj").objectReferenceValue = inputProvider;
                so.ApplyModifiedProperties();
            }

            // ---- 步骤 2：揉面（mini）----
            var kneadObj = new GameObject("Step_Kneading");
            var kneadStep = kneadObj.AddComponent<CraftStep_Kneading>();
            kneadObj.AddComponent<QualityEvaluator>();
            kneadObj.AddComponent<FeedbackController>();
            {
                var so = new SerializedObject(kneadStep);
                so.FindProperty("_stepId").stringValue = "knead_dough";
                so.FindProperty("_isMiniStep").boolValue = true;
                so.FindProperty("_config").objectReferenceValue = kneadConfig;
                so.FindProperty("_inputType").enumValueIndex = (int)CraftInputType.Circular;
                so.FindProperty("_anglePerUnit").floatValue = 360f;
                so.FindProperty("_maxCircles").floatValue = 5f;
                so.FindProperty("_minRadius").floatValue = 30f;
                so.FindProperty("_inputProviderObj").objectReferenceValue = inputProvider;
                so.ApplyModifiedProperties();
            }

            // ---- 步骤 3：蒸制（mini，不需按键——到达最佳时间自动判定，方便验证）----
            var steamObj = new GameObject("Step_Steaming");
            var steamStep = steamObj.AddComponent<CraftStep_Steaming>();
            steamObj.AddComponent<QualityEvaluator>();
            steamObj.AddComponent<FeedbackController>();
            {
                var so = new SerializedObject(steamStep);
                so.FindProperty("_stepId").stringValue = "steam_pastry";
                so.FindProperty("_isMiniStep").boolValue = true;
                so.FindProperty("_config").objectReferenceValue = steamConfig;
                so.FindProperty("_inputType").enumValueIndex = (int)CraftInputType.Timing;
                so.FindProperty("_optimalTime").floatValue = 5f;
                so.FindProperty("_toleranceTime").floatValue = 1.5f;
                so.FindProperty("_maxTime").floatValue = 30f;
                so.FindProperty("_requireButton").boolValue = false;
                so.FindProperty("_inputProviderObj").objectReferenceValue = inputProvider;
                so.ApplyModifiedProperties();
            }

            // ---- CraftManager（默认迷你模式 + 自动启动）----
            var mgrObj = new GameObject("CraftManager");
            mgrObj.AddComponent<AdaptiveDifficulty>();
            var mgr = mgrObj.AddComponent<CraftManager>();
            {
                var so = new SerializedObject(mgr);
                var steps = so.FindProperty("_steps");
                steps.arraySize = 3;
                steps.GetArrayElementAtIndex(0).objectReferenceValue = weighStep;
                steps.GetArrayElementAtIndex(1).objectReferenceValue = kneadStep;
                steps.GetArrayElementAtIndex(2).objectReferenceValue = steamStep;
                so.FindProperty("_autoStart").boolValue = false;
                so.FindProperty("_miniCraftMode").boolValue = true;
                so.ApplyModifiedProperties();
            }

            // ---- 测试驱动 ----
            var driverObj = new GameObject("T19TestDriver");
            var driver = driverObj.AddComponent<Test.T19MiniCraftTestDriver>();
            {
                var so = new SerializedObject(driver);
                so.FindProperty("_craftManager").objectReferenceValue = mgr;
                so.ApplyModifiedProperties();
            }

            EditorSceneManager.SaveScene(scene, "Assets/Scenes/T19MiniCraftTest.unity");
            AssetDatabase.SaveAssets();

            Debug.Log("T19 MiniCraft 验证场景创建完成: Assets/Scenes/T19MiniCraftTest.unity");
            Debug.Log("F=完整流程（称量→揉面→蒸制 3 步） M=迷你流程（揉面→蒸制 2 步，跳过称量）");
        }

        private static void EnsureFolders()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Settings/Craft"))
            {
                if (!AssetDatabase.IsValidFolder("Assets/Settings"))
                    AssetDatabase.CreateFolder("Assets", "Settings");
                AssetDatabase.CreateFolder("Assets/Settings", "Craft");
            }
        }

        private static StepConfig CreateConfig(string fileName, string stepId, string displayName,
            float target, float tolerance, float threshold, float weight)
        {
            var config = ScriptableObject.CreateInstance<StepConfig>();
            config.stepId = stepId;
            config.displayName = displayName;
            config.description = $"T19 MiniCraft 验证: {displayName}";
            config.targetValue = target;
            config.tolerance = tolerance;
            config.passThreshold = threshold;
            config.qualityWeight = weight;
            AssetDatabase.CreateAsset(config, $"Assets/Settings/Craft/{fileName}.asset");
            return config;
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
