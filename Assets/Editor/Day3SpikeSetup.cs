#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using VisualTreeAsset = UnityEngine.UIElements.VisualTreeAsset;
using UIDocument = UnityEngine.UIElements.UIDocument;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using PastryWorld.Codex;

namespace PastryWorld.Editor
{
    /// <summary>
    /// Day 3 Spike 批处理：生成纸张纹理 + 创建 UGUI/UI Toolkit 两个原型场景。
    /// </summary>
    public static class Day3SpikeSetup
    {
        const string k_ArtUI = "Assets/Art/UI";
        const string k_PaperTex = "Assets/Art/UI/TestPaperTexture.png";
        const string k_USS = "Assets/Settings/UI/codex-style.uss";
        const string k_UXML = "Assets/Settings/UI/codex-entry.uxml";

        public static void Run()
        {
            // 1. 生成纸张纹理
            GeneratePaperTexture();
            AssetDatabase.Refresh();
            Debug.Log("[Day3SpikeSetup] 纸张纹理已生成");

            // 2. 创建 UGUI 原型场景
            CreateUGUIScene();
            Debug.Log("[Day3SpikeSetup] UGUI 原型场景已创建");

            // 3. 创建 UI Toolkit 原型场景
            CreateUIToolkitScene();
            Debug.Log("[Day3SpikeSetup] UI Toolkit 原型场景已创建");

            // 4. 更新 BuildSettings
            var scenes = new EditorBuildSettingsScene[]
            {
                new EditorBuildSettingsScene("Assets/Scenes/Bootstrap.unity", true),
                new EditorBuildSettingsScene("Assets/Scenes/Main.unity", true),
                new EditorBuildSettingsScene("Assets/Scenes/InputTest.unity", false),
                new EditorBuildSettingsScene("Assets/Scenes/UISpike_UGUI.unity", false),
                new EditorBuildSettingsScene("Assets/Scenes/UISpike_UIToolkit.unity", false)
            };
            EditorBuildSettings.scenes = scenes;

            Debug.Log("[Day3SpikeSetup] 全部完成：纸张纹理 + UGUI原型 + UI Toolkit原型");
        }

        static void GeneratePaperTexture()
        {
            if (!AssetDatabase.IsValidFolder(k_ArtUI))
                AssetDatabase.CreateFolder("Assets/Art", "UI");

            int size = 1024;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float r = 245f / 255f;
                    float g = 235f / 255f;
                    float b = 210f / 255f;

                    // Perlin 噪声纸张纤维
                    float noise = Mathf.PerlinNoise(x * 0.02f, y * 0.02f) * 0.1f;
                    r += noise; g += noise; b += noise;

                    // 边缘暗化
                    float distToEdge = Mathf.Min(x, y, size - 1 - x, size - 1 - y);
                    float edgeDarken = Mathf.Clamp01(distToEdge / 80f);
                    r *= edgeDarken; g *= edgeDarken; b *= edgeDarken;

                    tex.SetPixel(x, y, new Color(r, g, b, 1f));
                }
            }
            tex.Apply();

            byte[] bytes = tex.EncodeToPNG();
            System.IO.File.WriteAllBytes(k_PaperTex, bytes);
            AssetDatabase.ImportAsset(k_PaperTex);
        }

        static void CreateUGUIScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Camera
            var camGo = new GameObject("Main Camera");
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5;
            camGo.transform.position = new Vector3(0, 0, -10);

            // EventSystem
            var esGo = new GameObject("EventSystem");
            esGo.AddComponent<EventSystem>();
            esGo.AddComponent<StandaloneInputModule>();

            // Canvas
            var canvasGo = new GameObject("Canvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            // CodexEntry panel
            var entryGo = new GameObject("CodexEntry");
            entryGo.transform.SetParent(canvasGo.transform, false);
            var entryRect = entryGo.AddComponent<RectTransform>();
            entryRect.anchorMin = new Vector2(0.5f, 0.5f);
            entryRect.anchorMax = new Vector2(0.5f, 0.5f);
            entryRect.pivot = new Vector2(0.5f, 0.5f);
            entryRect.anchoredPosition = Vector2.zero;
            entryRect.sizeDelta = new Vector2(600, 800);

            var entryImage = entryGo.AddComponent<Image>();
            var paperTex = AssetDatabase.LoadAssetAtPath<Texture2D>(k_PaperTex);
            if (paperTex != null)
            {
                entryImage.sprite = Sprite.Create(paperTex,
                    new Rect(0, 0, paperTex.width, paperTex.height),
                    Vector2.one * 0.5f);
            }
            entryImage.type = Image.Type.Sliced;

            // Title
            var titleGo = new GameObject("Title");
            titleGo.transform.SetParent(entryGo.transform, false);
            var titleRect = titleGo.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 1);
            titleRect.anchorMax = new Vector2(1, 1);
            titleRect.pivot = new Vector2(0.5f, 1);
            titleRect.anchoredPosition = new Vector2(0, -20);
            titleRect.sizeDelta = new Vector2(-40, 40);
            var titleText = titleGo.AddComponent<TextMeshProUGUI>();
            titleText.text = "桂花糕";
            titleText.fontSize = 24;
            titleText.color = new Color(0x2c / 255f, 0x18 / 255f, 0x10 / 255f);
            titleText.alignment = TextAlignmentOptions.TopLeft;

            // BodyText
            var bodyGo = new GameObject("BodyText");
            bodyGo.transform.SetParent(entryGo.transform, false);
            var bodyRect = bodyGo.AddComponent<RectTransform>();
            bodyRect.anchorMin = new Vector2(0, 0);
            bodyRect.anchorMax = new Vector2(1, 1);
            bodyRect.pivot = new Vector2(0.5f, 0.5f);
            bodyRect.offsetMin = new Vector2(20, 20);
            bodyRect.offsetMax = new Vector2(-20, -60);
            var bodyText = bodyGo.AddComponent<TextMeshProUGUI>();
            bodyText.text = "桂花糕是一种传统中式糕点，以糯米粉、桂花为原料，口感软糯香甜。";
            bodyText.fontSize = 14;
            bodyText.color = new Color(0x4a / 255f, 0x35 / 255f, 0x20 / 255f);
            bodyText.alignment = TextAlignmentOptions.TopLeft;

            // QualityMark
            var qmGo = new GameObject("QualityMark");
            qmGo.transform.SetParent(entryGo.transform, false);
            var qmRect = qmGo.AddComponent<RectTransform>();
            qmRect.anchorMin = new Vector2(1, 1);
            qmRect.anchorMax = new Vector2(1, 1);
            qmRect.pivot = new Vector2(1, 1);
            qmRect.anchoredPosition = new Vector2(-10, -10);
            qmRect.sizeDelta = new Vector2(32, 32);
            var qmImage = qmGo.AddComponent<Image>();
            qmImage.color = new Color(0.8f, 0.6f, 0.2f, 0.5f);

            // Page flip script
            var flip = entryGo.AddComponent<CodexPageFlipUGUI>();
            var so = new SerializedObject(flip);
            so.FindProperty("pageTransform").objectReferenceValue = entryRect;
            so.ApplyModifiedProperties();

            EditorSceneManager.SaveScene(scene, "Assets/Scenes/UISpike_UGUI.unity");
        }

        static void CreateUIToolkitScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Camera
            var camGo = new GameObject("Main Camera");
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5;
            camGo.transform.position = new Vector3(0, 0, -10);

            // UIDocument
            var uiDocGo = new GameObject("UIDocument");
            var uiDoc = uiDocGo.AddComponent<UIDocument>();
            var vta = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(k_UXML);
            if (vta != null)
            {
                uiDoc.visualTreeAsset = vta;
                Debug.Log("[Day3SpikeSetup] VisualTreeAsset 已赋值");
            }
            else
            {
                Debug.LogWarning("[Day3SpikeSetup] 无法加载 VisualTreeAsset，请检查 UXML 是否已导入");
            }

            // Page flip script
            uiDocGo.AddComponent<CodexPageFlipUITK>();

            EditorSceneManager.SaveScene(scene, "Assets/Scenes/UISpike_UIToolkit.unity");
        }
    }
}
#endif
