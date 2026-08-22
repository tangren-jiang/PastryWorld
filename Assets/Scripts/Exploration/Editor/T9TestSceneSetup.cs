using UnityEngine;
using UnityEngine.InputSystem;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using PastryWorld.Exploration.Test;

namespace PastryWorld.Exploration.Editor
{
    /// <summary>
    /// T9 测试场景构建脚本。创建 Player + 墙壁 + NPC + 边界。
    /// 运行：Unity batchmode -executeMethod PastryWorld.Exploration.Editor.T9TestSceneSetup.Setup
    /// </summary>
    public static class T9TestSceneSetup
    {
        [MenuItem("Tools/PastryWorld/T9 Test Scene Setup")]
        public static void Setup()
        {
            // 先设置碰撞层
            CollisionLayerSetup.Setup();

            // 创建场景
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 查找 InputActionAsset
            var inputAssetGUIDs = AssetDatabase.FindAssets("PastryWorldInput t:InputActionAsset");
            InputActionAsset inputAsset = null;
            if (inputAssetGUIDs.Length > 0)
            {
                var path = AssetDatabase.GUIDToAssetPath(inputAssetGUIDs[0]);
                inputAsset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(path);
            }

            // 主相机
            var camObj = new GameObject("MainCamera");
            var cam = camObj.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5f;
            camObj.transform.position = new Vector3(0, 0, -10);
            camObj.tag = "MainCamera";

            // 创建白色方格 Sprite（占位）
            var tex = CreateWhiteTexture(32, 32);
            var sprite = Sprite.Create(tex, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 32);

            // 玩家
            var playerObj = new GameObject("Player");
            playerObj.layer = 8; // Character
            playerObj.transform.position = Vector3.zero;
            var playerSR = playerObj.AddComponent<SpriteRenderer>();
            playerSR.sprite = sprite;
            playerSR.color = new Color(0.4f, 0.8f, 1f); // 浅蓝色
            var playerRB = playerObj.AddComponent<Rigidbody2D>();
            playerRB.bodyType = RigidbodyType2D.Kinematic;
            playerRB.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            var playerCol = playerObj.AddComponent<BoxCollider2D>();
            playerCol.size = new Vector2(0.8f, 0.8f);
            var playerCtrl = playerObj.AddComponent<PlayerController>();
            if (inputAsset != null)
            {
                var so = new SerializedObject(playerCtrl);
                so.FindProperty("_inputActions").objectReferenceValue = inputAsset;
                so.ApplyModifiedProperties();
            }

            // 墙壁（Environment 层）
            CreateWall("Wall_Top", new Vector2(0, 3), new Vector2(10, 0.5f), sprite, new Color(0.6f, 0.5f, 0.3f), 9);
            CreateWall("Wall_Bottom", new Vector2(0, -3), new Vector2(10, 0.5f), sprite, new Color(0.6f, 0.5f, 0.3f), 9);
            CreateWall("Wall_Left", new Vector2(-5, 0), new Vector2(0.5f, 6), sprite, new Color(0.6f, 0.5f, 0.3f), 9);
            CreateWall("Wall_Right", new Vector2(5, 0), new Vector2(0.5f, 6), sprite, new Color(0.6f, 0.5f, 0.3f), 9);

            // NPC（Interactable 层）
            var npcObj = new GameObject("NPC_HoneyWitch");
            npcObj.layer = 10; // Interactable
            npcObj.transform.position = new Vector3(2, 0, 0);
            var npcSR = npcObj.AddComponent<SpriteRenderer>();
            npcSR.sprite = sprite;
            npcSR.color = new Color(1f, 0.8f, 0.3f); // 暖黄色
            var npcCol = npcObj.AddComponent<BoxCollider2D>();
            npcCol.size = new Vector2(0.8f, 0.8f);
            npcCol.isTrigger = true;
            npcObj.AddComponent<TestNPC>();

            // 边界（Boundary 层）- 更大的外圈
            CreateWall("Boundary_Top", new Vector2(0, 6), new Vector2(15, 0.5f), sprite, new Color(0.3f, 0.3f, 0.3f), 12);
            CreateWall("Boundary_Bottom", new Vector2(0, -6), new Vector2(15, 0.5f), sprite, new Color(0.3f, 0.3f, 0.3f), 12);
            CreateWall("Boundary_Left", new Vector2(-8, 0), new Vector2(0.5f, 12), sprite, new Color(0.3f, 0.3f, 0.3f), 12);
            CreateWall("Boundary_Right", new Vector2(8, 0), new Vector2(0.5f, 12), sprite, new Color(0.3f, 0.3f, 0.3f), 12);

            // 保存场景
            string scenePath = "Assets/Scenes/ExplorationTest.unity";
            // 确保目录存在
            if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
                AssetDatabase.CreateFolder("Assets", "Scenes");
            EditorSceneManager.SaveScene(scene, scenePath);

            Debug.Log("T9 测试场景创建完成: " + scenePath);
            Debug.Log("Player=Character(8), Walls=Environment(9), NPC=Interactable(10), Boundary=Boundary(12)");
        }

        private static GameObject CreateWall(string name, Vector2 pos, Vector2 size, Sprite sprite, Color color, int layer)
        {
            var obj = new GameObject(name);
            obj.layer = layer;
            obj.transform.position = pos;
            obj.transform.localScale = size;
            var sr = obj.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = color;
            var col = obj.AddComponent<BoxCollider2D>();
            col.size = Vector2.one;
            return obj;
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
