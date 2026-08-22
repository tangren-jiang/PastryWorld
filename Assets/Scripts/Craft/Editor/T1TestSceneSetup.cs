using UnityEngine;
using UnityEngine.InputSystem;
using UnityEditor;
using UnityEditor.SceneManagement;
using PastryWorld.Craft;
using PastryWorld.Input;

namespace PastryWorld.Craft.Editor
{
    /// <summary>
    /// T1 工艺架构测试场景构建脚本。
    /// 创建称量工艺步骤 + CraftManager + 测试环境。
    /// 运行：Unity batchmode -executeMethod PastryWorld.Craft.Editor.T1TestSceneSetup.Setup
    /// </summary>
    public static class T1TestSceneSetup
    {
        [MenuItem("Tools/PastryWorld/T1 Craft Test Scene Setup")]
        public static void Setup()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 主相机
            var camObj = new GameObject("MainCamera");
            var cam = camObj.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5f;
            camObj.transform.position = new Vector3(0, 0, -10);
            camObj.tag = "MainCamera";

            // 白色纹理
            var tex = CreateWhiteTexture(32, 32);
            var sprite = Sprite.Create(tex, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 32);

            // 容器外框
            var containerObj = new GameObject("Container");
            containerObj.transform.position = new Vector3(-2, 0, 0);
            var containerSR = containerObj.AddComponent<SpriteRenderer>();
            containerSR.sprite = sprite;
            containerSR.color = new Color(0.8f, 0.8f, 0.8f, 0.3f);
            containerObj.transform.localScale = new Vector3(2f, 3f, 1f);
            var containerCol = containerObj.AddComponent<BoxCollider2D>();
            containerCol.size = Vector2.one;

            // 填充物（显示重量进度）
            var fillObj = new GameObject("Fill");
            fillObj.transform.SetParent(containerObj.transform);
            fillObj.transform.localPosition = new Vector3(0, -1.5f, 0);
            fillObj.transform.localScale = new Vector3(0.9f, 0.01f, 1f);
            var fillSR = fillObj.AddComponent<SpriteRenderer>();
            fillSR.sprite = sprite;
            fillSR.color = new Color(1f, 0.8f, 0.3f); // 暖黄色

            // 目标线（目标重量标记）
            var targetLineObj = new GameObject("TargetLine");
            targetLineObj.transform.SetParent(containerObj.transform);
            float targetRatio = 100f / 200f; // target/maxWeight
            targetLineObj.transform.localPosition = new Vector3(0, -1.5f + 3f * targetRatio, 0);
            targetLineObj.transform.localScale = new Vector3(1f, 0.05f, 1f);
            var targetSR = targetLineObj.AddComponent<SpriteRenderer>();
            targetSR.sprite = sprite;
            targetSR.color = new Color(0.2f, 0.8f, 0.3f, 0.6f); // 绿色目标线

            // 鼠标输入提供者
            var inputObj = new GameObject("MouseInput");
            var mouseProvider = inputObj.AddComponent<MouseInputProvider>();

            // 品质判定器
            var evaluatorObj = new GameObject("QualityEvaluator");
            var evaluator = evaluatorObj.AddComponent<QualityEvaluator>();

            // 反馈控制器
            var feedbackObj = new GameObject("FeedbackController");
            var feedback = feedbackObj.AddComponent<FeedbackController>();

            // 创建 StepConfig SO
            var stepConfig = ScriptableObject.CreateInstance<StepConfig>();
            stepConfig.stepId = "weigh_honey";
            stepConfig.displayName = "称量蜂蜜";
            stepConfig.description = "拖动鼠标倾倒蜂蜜，接近 100g 目标";
            stepConfig.targetValue = 100f;
            stepConfig.tolerance = 15f;
            stepConfig.passThreshold = 0.6f;
            stepConfig.duration = 20f;
            stepConfig.qualityWeight = 1f;
            stepConfig.inputSensitivity = 1f;

            // 制作步骤 GameObject
            var stepObj = new GameObject("Step_Weighing");
            stepObj.transform.position = new Vector3(-2, 0, 0);
            var step = stepObj.AddComponent<CraftStep_Weighing>();
            var stepFeedback = stepObj.AddComponent<FeedbackController>();
            var stepEvaluator = stepObj.AddComponent<QualityEvaluator>();

            // 用 SerializedObject 设置 private 字段
            var so = new SerializedObject(step);
            so.FindProperty("_stepId").stringValue = "weigh_honey";
            so.FindProperty("_isMiniStep").boolValue = false;
            so.FindProperty("_config").objectReferenceValue = stepConfig;
            so.FindProperty("_inputType").enumValueIndex = (int)CraftInputType.Drag;
            so.FindProperty("_pourRate").floatValue = 50f;
            so.FindProperty("_maxWeight").floatValue = 200f;
            so.FindProperty("_inputProviderObj").objectReferenceValue = mouseProvider;
            so.FindProperty("_fillRenderer").objectReferenceValue = fillSR;
            // 让 step 使用自己的 evaluator 和 feedback
            // (CraftStep.Awake 会 GetComponent 获取)
            so.ApplyModifiedProperties();

            // CraftManager + AdaptiveDifficulty（T7 失败自适应，纯后台）
            var mgrObj = new GameObject("CraftManager");
            mgrObj.AddComponent<AdaptiveDifficulty>();
            var mgr = mgrObj.AddComponent<CraftManager>();
            var mgrSO = new SerializedObject(mgr);
            var stepsProp = mgrSO.FindProperty("_steps");
            stepsProp.arraySize = 1;
            stepsProp.GetArrayElementAtIndex(0).objectReferenceValue = step;
            mgrSO.FindProperty("_autoStart").boolValue = true;
            mgrSO.ApplyModifiedProperties();

            // 保存 SO 资产
            if (!AssetDatabase.IsValidFolder("Assets/Settings/Craft"))
                AssetDatabase.CreateFolder("Assets/Settings", "Craft");
            AssetDatabase.CreateAsset(stepConfig, "Assets/Settings/Craft/StepConfig_WeighHoney.asset");

            // 保存场景
            if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
                AssetDatabase.CreateFolder("Assets", "Scenes");
            string scenePath = "Assets/Scenes/CraftTest.unity";
            EditorSceneManager.SaveScene(scene, scenePath);
            AssetDatabase.SaveAssets();

            Debug.Log("T1 工艺架构测试场景创建完成: " + scenePath);
            Debug.Log("称量步骤: 目标100g, 容差15g, 倾倒速率50/s");
            Debug.Log("操作: 按住鼠标左键倾倒，松开判定");
        }

        private static Texture2D CreateWhiteTexture(int w, int h)
        {
            var tex = new Texture2D(w, h);
            var pixels = new Color[w * h];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = Color.white;
            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }
    }
}
