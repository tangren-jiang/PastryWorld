using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;

namespace PastryWorld.Editor
{
    /// <summary>
    /// W0 修复：为 InputTest 场景的 TestSprite 生成并分配白色方块贴图。
    /// 一次性脚本，运行后可删除。
    /// </summary>
    public static class FixTestSprite
    {
        [MenuItem("Tools/W0/修复 TestSprite 贴图")]
        public static void Run()
        {
            // 1. 生成 64x64 白色 PNG
            const string dir = "Assets/Art/Test";
            const string pngPath = dir + "/WhiteSquare.png";
            if (!AssetDatabase.IsValidFolder(dir))
            {
                if (!AssetDatabase.IsValidFolder("Assets/Art"))
                    AssetDatabase.CreateFolder("Assets", "Art");
                AssetDatabase.CreateFolder("Assets/Art", "Test");
            }

            var tex = new Texture2D(64, 64, TextureFormat.RGBA32, false);
            var pixels = new Color32[64 * 64];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = new Color32(255, 255, 255, 255);
            tex.SetPixels32(pixels);
            tex.Apply();
            System.IO.File.WriteAllBytes(pngPath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(pngPath, ImportAssetOptions.ForceUpdate);

            // 2. 设为 Sprite 导入
            var importer = (TextureImporter)AssetImporter.GetAtPath(pngPath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();

            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(pngPath);
            Debug.Log($"[FixTestSprite] sprite loaded: {sprite != null}");

            // 3. 打开 InputTest 场景并赋值
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/InputTest.unity",
                OpenSceneMode.Single);
            var sr = Object.FindObjectOfType<SpriteRenderer>();
            if (sr == null)
            {
                Debug.LogError("[FixTestSprite] 场景中找不到 SpriteRenderer！");
                return;
            }
            sr.sprite = sprite;
            sr.color = Color.white;
            sr.sortingOrder = 10;

            // 4. 保险：确认相机在 Sprite 前方
            var cam = Camera.main;
            if (cam != null && cam.transform.position.z >= -1f)
                cam.transform.position = new Vector3(0, 0, -10);

            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[FixTestSprite] 完成：{sr.gameObject.name} -> {sprite.name}，相机 z={cam?.transform.position.z}");
        }
    }
}
