#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace PastryWorld.Editor
{
    /// <summary>
    /// W0 一次性环境配置脚本（batchmode 执行）。
    /// 对应 W0-EXEC 手册 Day 1 的 GUI 操作的程序化等价物。
    /// </summary>
    public static class W0Setup
    {
        public static void Run()
        {
            Debug.Log("[W0Setup] 开始");

            EnsureFolders();

            // === 1. URP 2D 管线资产 ===
            var rendererData = ScriptableObject.CreateInstance<Renderer2DData>();
            AssetDatabase.CreateAsset(rendererData, "Assets/Settings/URP/Renderer2DData.asset");

            var urpAsset = ScriptableObject.CreateInstance<UniversalRenderPipelineAsset>();
            var so = new SerializedObject(urpAsset);
            var list = so.FindProperty("m_RendererDataList");
            list.arraySize = 1;
            list.GetArrayElementAtIndex(0).objectReferenceValue = rendererData;
            so.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(urpAsset, "Assets/Settings/URP/PastryWorldURP.asset");
            AssetDatabase.SaveAssets();

            // === 2. Graphics + Quality 引用 URP ===
            GraphicsSettings.defaultRenderPipeline = urpAsset;
            QualitySettings.renderPipeline = urpAsset; // 当前默认等级

            // 质量等级整理：只留 Mobile / PC 两级
            // (默认6级：索引1=Low→Mobile, 索引2=Medium→PC，其余删除)
            RenameLevel(1, "Mobile");
            RenameLevel(2, "PC");
            // 删除时从高索引往低索引删（0,3,4,5）
            QualitySettings.SetQualityLevel(2, false); // 先切到 PC(2) 避免删到当前
            DeleteLevel(5); DeleteLevel(4); DeleteLevel(3); DeleteLevel(0);
            // 删除后剩两级：0=Mobile? 顺序保持相对：删0后 Low(原1)变0，Medium变1
            QualitySettings.SetQualityLevel(1, true); // 默认 PC
            Debug.Log("[W0Setup] 质量等级数量=" + QualitySettings.names.Length +
                      " 名称=" + string.Join(",", QualitySettings.names));

            // === 3. Player Settings ===
            PlayerSettings.companyName = "PastryWorldStudio";
            PlayerSettings.productName = "PastryWorld";
            // Active Input Handling = Both (1=New, 2=Both)
            var playerSettingsAsset = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0];
            var pso = new SerializedObject(playerSettingsAsset);
            var handler = pso.FindProperty("activeInputHandler");
            if (handler != null) { handler.intValue = 2; pso.ApplyModifiedPropertiesWithoutUndo(); }

            // === 4. 场景 ===
            CreateBootstrapScene();
            CreateMainScene();
            CreateInputTestScene();

            // === 5. Build Settings 场景列表 ===
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene("Assets/Scenes/Bootstrap.unity", true),
                new EditorBuildSettingsScene("Assets/Scenes/Main.unity", true),
            };

            AssetDatabase.SaveAssets();
            Debug.Log("[W0Setup] 完成 ✅");
        }

        static void EnsureFolders()
        {
            string[] folders =
            {
                "Assets/Settings", "Assets/Settings/URP", "Assets/Settings/Input", "Assets/Settings/UI",
                "Assets/Scenes", "Assets/Art", "Assets/Art/UI",
            };
            foreach (var f in folders)
                if (!AssetDatabase.IsValidFolder(f))
                    AssetDatabase.CreateFolder(
                        System.IO.Path.GetDirectoryName(f).Replace('\\', '/'),
                        System.IO.Path.GetFileName(f));
        }

        static void RenameLevel(int index, string newName)
        {
            var qs = new SerializedObject(QualitySettings.GetQualitySettings());
            var levels = qs.FindProperty("m_QualitySettings");
            if (index < levels.arraySize)
            {
                var item = levels.GetArrayElementAtIndex(index);
                item.FindPropertyRelative("name").stringValue = newName;
                qs.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        static void DeleteLevel(int index)
        {
            var qs = new SerializedObject(QualitySettings.GetQualitySettings());
            var levels = qs.FindProperty("m_QualitySettings");
            if (index < levels.arraySize)
            {
                levels.DeleteArrayElementAtIndex(index);
                qs.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        static void CreateBootstrapScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            var go = new GameObject("Bootstrap");
            go.AddComponent<PastryWorld.Core.BootstrapLoader>();
            EditorSceneManager.SaveScene(scene, "Assets/Scenes/Bootstrap.unity");
            Debug.Log("[W0Setup] Bootstrap.unity 已创建");
        }

        static void CreateMainScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var camGo = new GameObject("Main Camera");
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5.4f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.96f, 0.93f, 0.86f); // 暖米色底
            camGo.tag = "MainCamera";
            camGo.AddComponent<AudioListener>();
            new GameObject("GameManager");
            EditorSceneManager.SaveScene(scene, "Assets/Scenes/Main.unity");
            Debug.Log("[W0Setup] Main.unity 已创建");
        }

        static void CreateInputTestScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var camGo = new GameObject("Main Camera");
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5.4f;
            camGo.tag = "MainCamera";
            camGo.AddComponent<AudioListener>();

            var spriteGo = new GameObject("TestSprite");
            var sr = spriteGo.AddComponent<SpriteRenderer>();
            sr.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("Square.png");

            var providerGo = new GameObject("MouseInputProvider");
            providerGo.AddComponent<PastryWorld.Input.MouseInputProvider>();

            var ctrl = spriteGo.AddComponent<global::InputTestController>();
            var ctrlSO = new SerializedObject(ctrl);
            ctrlSO.FindProperty("_inputProvider").objectReferenceValue =
                providerGo.GetComponent<PastryWorld.Input.MouseInputProvider>();
            ctrlSO.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, "Assets/Scenes/InputTest.unity");
            Debug.Log("[W0Setup] InputTest.unity 已创建（Sprite跟鼠标）");
        }
    }
}
#endif
