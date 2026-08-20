#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

public class PaperTextureGenerator : MonoBehaviour
{
    [MenuItem("Tools/Generate Paper Texture")]
    static void Generate()
    {
        int size = 1024;
        Texture2D tex = new Texture2D(size, size);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                // 泛黄底色
                float r = 245f / 255f;
                float g = 235f / 255f;
                float b = 210f / 255f;

                // Perlin噪声纸张纤维
                float noise = Mathf.PerlinNoise(x * 0.02f, y * 0.02f) * 0.1f;
                r += noise; g += noise; b += noise;

                tex.SetPixel(x, y, new Color(r, g, b));
            }
        }
        tex.Apply();

        // 保存
        byte[] bytes = tex.EncodeToPNG();
        System.IO.File.WriteAllBytes(
            "Assets/Art/UI/TestPaperTexture.png", bytes);
        AssetDatabase.Refresh();

        Debug.Log("纸张纹理已生成");
    }
}
#endif
