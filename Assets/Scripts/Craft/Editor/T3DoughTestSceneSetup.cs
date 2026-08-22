#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using PastryWorld.Craft.Dough;
using PastryWorld.Craft;
using PastryWorld.Input;

namespace PastryWorld.Editor
{
    /// <summary>
    /// T3 面团物理测试场景搭建。
    /// 菜单：Tools/PastryWorld/T3 Dough Test Scene
    /// </summary>
    public static class T3DoughTestSceneSetup
    {
        [MenuItem("Tools/PastryWorld/T3 Dough Test Scene", false, 30)]
        public static void SetupScene()
        {
            // 创建新场景
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 主相机
            var camObj = new GameObject("Main Camera");
            camObj.tag = "MainCamera";
            var cam = camObj.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5f;
            camObj.transform.position = new Vector3(0, 0, -10);
            camObj.AddComponent<AudioListener>();

            // 面团容器
            var doughObj = new GameObject("Dough");
            doughObj.transform.position = Vector3.zero;

            // MeshFilter + MeshRenderer
            var meshFilter = doughObj.AddComponent<MeshFilter>();
            var meshRenderer = doughObj.AddComponent<MeshRenderer>();

            // 创建配置
            var config = ScriptableObject.CreateInstance<DoughPhysicsConfig>();
            AssetDatabase.CreateAsset(config, "Assets/ScriptableObjects/DoughPhysicsConfig_T3.asset");

            // 创建材质
            var doughMat = new Material(Shader.Find("PastryWorld/Dough"));
            if (doughMat.shader == null || doughMat.shader.name == "Hidden/InternalErrorShader")
            {
                Debug.LogWarning("[T3] PastryWorld/Dough 着色器未找到，使用 URP/Unlit 作为替代");
                doughMat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            }

            // 生成程序化纹理（纯色圆形）
            var baseTex = CreateCircleTexture(128, new Color(0.82f, 0.72f, 0.55f, 1f));
            AssetDatabase.CreateAsset(baseTex, "Assets/Art/Dough_BaseTex.asset");
            doughMat.SetTexture("_BaseMap", baseTex);

            // 光滑度遮罩（灰度渐变）
            var smoothMask = CreateRadialGradient(128, 0.3f, 1.0f);
            AssetDatabase.CreateAsset(smoothMask, "Assets/Art/Dough_SmoothMask.asset");
            doughMat.SetTexture("_SmoothnessMap", smoothMask);

            AssetDatabase.CreateAsset(doughMat, "Assets/Art/Dough_Material.mat");
            meshRenderer.material = doughMat;

            // 组件挂载
            var deformer = doughObj.AddComponent<DoughMeshDeformer>();
            var materialCtrl = doughObj.AddComponent<DoughMaterialController>();
            var bounce = doughObj.AddComponent<DoughBounceEffect>();

            // 设置配置
            var configField = typeof(DoughMeshDeformer).GetField("_config",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (configField != null) configField.SetValue(deformer, config);

            var matConfigField = typeof(DoughMaterialController).GetField("_config",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (matConfigField != null) matConfigField.SetValue(materialCtrl, config);

            var bounceConfigField = typeof(DoughBounceEffect).GetField("_config",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (bounceConfigField != null) bounceConfigField.SetValue(bounce, config);

            // 蒸汽粒子
            var steamObj = new GameObject("Steam");
            steamObj.transform.SetParent(doughObj.transform);
            steamObj.transform.localPosition = new Vector3(0, 1f, 0);
            var ps = steamObj.AddComponent<ParticleSystem>();
            var steamCtrl = steamObj.AddComponent<SteamParticleController>();
            var mainModule = ps.main;
            mainModule.startLifetime = 2f;
            mainModule.startSpeed = 1f;
            mainModule.startSize = 0.3f;
            mainModule.startColor = new Color(1f, 1f, 1f, 0.3f);
            mainModule.maxParticles = 100;
            var shapeModule = ps.shape;
            shapeModule.enabled = true;
            shapeModule.shapeType = ParticleSystemShapeType.Circle;
            shapeModule.radius = 0.5f;
            var emissionModule = ps.emission;
            emissionModule.rateOverTime = 0;
            var renderer = steamObj.GetComponent<ParticleSystemRenderer>();
            if (renderer != null)
            {
                renderer.sortingOrder = 10;
            }

            // DoughController
            var doughCtrl = doughObj.AddComponent<DoughController>();
            var dcDeformerField = typeof(DoughController).GetField("_deformer",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var dcMatField = typeof(DoughController).GetField("_material",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var dcSteamField = typeof(DoughController).GetField("_steam",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var dcBounceField = typeof(DoughController).GetField("_bounce",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var dcConfigField = typeof(DoughController).GetField("_config",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (dcDeformerField != null) dcDeformerField.SetValue(doughCtrl, deformer);
            if (dcMatField != null) dcMatField.SetValue(doughCtrl, materialCtrl);
            if (dcSteamField != null) dcSteamField.SetValue(doughCtrl, steamCtrl);
            if (dcBounceField != null) dcBounceField.SetValue(doughCtrl, bounce);
            if (dcConfigField != null) dcConfigField.SetValue(doughCtrl, config);

            // 输入提供者
            var inputObj = new GameObject("InputManager");
            var inputMgr = inputObj.AddComponent<InputManager>();
            var mouseProvider = inputObj.AddComponent<MouseInputProvider>();

            // 揉面步骤测试对象
            var kneadObj = new GameObject("KneadStep");
            kneadObj.transform.position = Vector3.zero;
            var knead = kneadObj.AddComponent<CraftStep_Kneading>();
            var stepConfig = ScriptableObject.CreateInstance<StepConfig>();
            stepConfig.stepId = "knead_test";
            stepConfig.targetValue = 3f;
            stepConfig.tolerance = 1f;
            stepConfig.passThreshold = 0.5f;
            AssetDatabase.CreateAsset(stepConfig, "Assets/ScriptableObjects/StepConfig_KneadT3.asset");

            // 用 SerializedObject 设置 private fields
            SetPrivateField(knead, "_inputProviderObj", mouseProvider);
            SetPrivateField(knead, "_mainCamera", cam);
            SetPrivateField(knead, "_doughController", doughCtrl);
            SetPrivateField(knead, "_config", stepConfig);
            SetPrivateField(knead, "_inputType", CraftInputType.Circular);

            // 品质评估器
            var evaluator = kneadObj.AddComponent<QualityEvaluator>();

            // 保存场景
            string scenePath = "Assets/Scenes/DoughTest.unity";
            EditorSceneManager.SaveScene(scene, scenePath);
            Debug.Log($"[T3] 面团物理测试场景已创建: {scenePath}");
            Debug.Log("[T3] 操作说明：按住鼠标在面团上画圆圈揉面，观察形变和光滑度变化");

            AssetDatabase.SaveAssets();
            EditorUtility.FocusProjectWindow();
        }

        private static void SetPrivateField(object obj, string fieldName, object value)
        {
            var field = obj.GetType().GetField(fieldName,
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                field.SetValue(obj, value);
            }
            else
            {
                var serializedObject = new SerializedObject((UnityEngine.Object)obj);
                var prop = serializedObject.FindProperty(fieldName);
                if (prop != null)
                {
                    if (prop.propertyType == SerializedPropertyType.ObjectReference)
                        prop.objectReferenceValue = (UnityEngine.Object)value;
                    else if (prop.propertyType == SerializedPropertyType.Enum)
                        prop.enumValueIndex = (int)value;
                    serializedObject.ApplyModifiedProperties();
                }
            }
        }

        private static Texture2D CreateCircleTexture(int size, Color color)
        {
            var tex = new Texture2D(size, size);
            float center = size * 0.5f;
            float radius = size * 0.45f;
            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    tex.SetPixel(x, y, dist <= radius ? color : new Color(0, 0, 0, 0));
                }
            }
            tex.Apply();
            tex.filterMode = FilterMode.Bilinear;
            return tex;
        }

        private static Texture2D CreateRadialGradient(int size, float centerValue, float edgeValue)
        {
            var tex = new Texture2D(size, size, TextureFormat.R8, false);
            float center = size * 0.5f;
            float radius = size * 0.5f;
            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    float t = Mathf.Clamp01(dist / radius);
                    float v = Mathf.Lerp(centerValue, edgeValue, t);
                    tex.SetPixel(x, y, new Color(v, v, v, 1f));
                }
            }
            tex.Apply();
            tex.filterMode = FilterMode.Bilinear;
            return tex;
        }
    }
}
#endif
