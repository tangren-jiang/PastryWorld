using UnityEngine;
using UnityEngine.InputSystem;
using UnityEditor;
using UnityEditor.SceneManagement;
using PastryWorld.Exploration.Test;

namespace PastryWorld.Exploration.Editor
{
    /// <summary>
    /// W2 测试场景构建脚本（T10 弱引导 + T11 视差 + T12 水墨转场）。
    /// 生成 6 层视差背景 + 玩家 + 相机跟随 + 引导微光粒子 + NPC + 转场触发器。
    /// 运行：Unity batchmode -executeMethod PastryWorld.Exploration.Editor.W2TestSceneSetup.Setup
    /// 验证要点：
    /// - T11：移动玩家（WASD/方向键），背景层按 factor 不同速度移动
    /// - T10：180 秒无交互后萤光粒子增强；按 E 与 NPC 交互重置
    /// - T12：按 T 触发水墨转场
    /// </summary>
    public static class W2TestSceneSetup
    {
        [MenuItem("Tools/PastryWorld/W2 Test Scene Setup (T10+T11+T12)")]
        public static void Setup()
        {
            CollisionLayerSetup.Setup();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // InputActionAsset
            var inputAssetGUIDs = AssetDatabase.FindAssets("PastryWorldInput t:InputActionAsset");
            InputActionAsset inputAsset = null;
            if (inputAssetGUIDs.Length > 0)
            {
                inputAsset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(
                    AssetDatabase.GUIDToAssetPath(inputAssetGUIDs[0]));
            }

            var sprite = CreateSquareSprite(64, Color.white);

            // ---- T11 视差层（factor 大=远景，小=近景）----
            CreateParallaxLayer("Parallax_Sky", 0.95f, -20, new Color(0.45f, 0.62f, 0.78f),
                new Vector2(40, 22), ParallaxLayer.LayerTier.Essential, sprite, 0);
            CreateParallaxLayer("Parallax_FarMountain", 0.8f, -15, new Color(0.55f, 0.68f, 0.72f),
                new Vector2(36, 16), ParallaxLayer.LayerTier.Essential, sprite, 1);
            CreateParallaxLayer("Parallax_MidHill", 0.55f, -10, new Color(0.62f, 0.74f, 0.64f),
                new Vector2(32, 12), ParallaxLayer.LayerTier.Essential, sprite, 2);
            CreateParallaxLayer("Parallax_MidTree", 0.4f, -8, new Color(0.42f, 0.6f, 0.45f),
                new Vector2(30, 10), ParallaxLayer.LayerTier.Essential, sprite, 3);
            // 装饰层：移动端隐藏（编辑器看不到降级效果，PC 桌面可见）
            CreateParallaxLayer("Parallax_DecorCloud", 0.7f, -12, new Color(1f, 1f, 1f, 0.4f),
                new Vector2(24, 5), ParallaxLayer.LayerTier.Decorative, sprite, 4);
            CreateParallaxLayer("Parallax_NearGrass", 0.15f, -6, new Color(0.3f, 0.5f, 0.35f),
                new Vector2(28, 6), ParallaxLayer.LayerTier.Essential, sprite, 5);

            // ---- 相机（跟随玩家，视差效果的移动源）----
            var camObj = new GameObject("MainCamera");
            var cam = camObj.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5f;
            camObj.transform.position = new Vector3(0, 0, -10);
            camObj.tag = "MainCamera";
            camObj.AddComponent<ParallaxCamera>();

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

            // ---- T10 引导管理器 + 环境微光粒子 ----
            var guidanceObj = new GameObject("GuidanceManager");
            guidanceObj.AddComponent<GuidanceManager>();

            var ambienceObj = new GameObject("GuidanceAmbience_Fireflies");
            ambienceObj.transform.position = new Vector3(0, 0, -5);
            var ps = ambienceObj.AddComponent<ParticleSystem>();
            SetupFireflyParticles(ps, sprite);
            ambienceObj.AddComponent<GuidanceAmbienceEffect>();

            // ---- NPC（有效交互 → 重置引导计时）----
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
            npcObj.AddComponent<TestNPC>();

            // ---- T12 水墨转场（挂到场景常驻实例，按 T 演示）----
            var inkObj = new GameObject("InkTransitionController");
            var inkCtrl = inkObj.AddComponent<InkTransitionController>();
            var inkShader = Shader.Find("PastryWorld/InkTransition");
            if (inkShader != null)
            {
                var so2 = new SerializedObject(inkCtrl);
                so2.FindProperty("_inkShader").objectReferenceValue = inkShader;
                so2.ApplyModifiedProperties();
            }
            inkObj.AddComponent<Test.W2TransitionDemo>();

            // ---- 地面（Environment 层，供碰撞）----
            CreateWall("Ground", new Vector2(0, -2), new Vector2(24, 0.5f), sprite,
                new Color(0.5f, 0.4f, 0.3f), 9, 10);
            CreateWall("Boundary_Left", new Vector2(-12, 0), new Vector2(0.5f, 14), sprite,
                new Color(0.3f, 0.3f, 0.3f), 12, 10);
            CreateWall("Boundary_Right", new Vector2(12, 0), new Vector2(0.5f, 14), sprite,
                new Color(0.3f, 0.3f, 0.3f), 12, 10);

            if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
                AssetDatabase.CreateFolder("Assets", "Scenes");
            EditorSceneManager.SaveScene(scene, "Assets/Scenes/W2Test.unity");

            Debug.Log("W2 测试场景创建完成: Assets/Scenes/W2Test.unity");
            Debug.Log("T11: WASD 移动看视差；T10: 等 180s 或调 DebugSetIdleTime 看萤光增强，E 交互重置；T12: 按 T 触发水墨转场");
        }

        private static void CreateParallaxLayer(string name, float factor, float z, Color color,
            Vector2 scale, ParallaxLayer.LayerTier tier, Sprite sprite, int sortingOrder)
        {
            var obj = new GameObject(name);
            obj.transform.position = new Vector3(0, 0, z);
            var sr = obj.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = color;
            sr.sortingOrder = sortingOrder;
            obj.transform.localScale = new Vector3(scale.x, scale.y, 1);
            var layer = obj.AddComponent<ParallaxLayer>();
            var so = new SerializedObject(layer);
            so.FindProperty("_parallaxFactor").floatValue = factor;
            so.FindProperty("_tier").enumValueIndex = (int)tier;
            so.ApplyModifiedProperties();
        }

        private static void SetupFireflyParticles(ParticleSystem ps, Sprite sprite)
        {
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(3f, 6f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.1f, 0.4f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.18f);
            main.startColor = new Color(1f, 0.95f, 0.6f, 0.8f); // 暖黄萤光
            main.maxParticles = 200;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.loop = true;

            var emission = ps.emission;
            emission.rateOverTime = 6f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(16f, 6f, 0f);

            // 萤光闪烁
            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            colorOverLifetime.color = new Gradient
            {
                alphaKeys = new[]
                {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(0.9f, 0.2f),
                    new GradientAlphaKey(0.5f, 0.7f),
                    new GradientAlphaKey(0f, 1f)
                }
            };

            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.material = new Material(Shader.Find("Sprites/Default"))
            {
                mainTexture = sprite.texture
            };
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
