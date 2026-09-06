using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PastryWorld.Integration.Editor
{
    /// <summary>
    /// 一次性修复工具：给各测试场景补 AudioListener。
    /// 背景：测试场景由编辑器脚本生成，相机未挂 AudioListener，运行时刷
    /// "There are no audio listeners in the scene" 警告且 BGM 无声。
    /// 每个场景只在完全没有 AudioListener 时给第一个相机补一个（多相机场景避免双 Listener）。
    /// </summary>
    public static class TestSceneAudioFixer
    {
        private static readonly string[] Scenes =
        {
            "Assets/Scenes/CraftTest.unity",
            "Assets/Scenes/ExplorationTest.unity",
            "Assets/Scenes/W5CodexTest.unity",
            "Assets/Scenes/W6IntegrationTest.unity",
            "Assets/Scenes/W3NarrativeTest.unity",
            "Assets/Scenes/W4RealityTest.unity",
            "Assets/Scenes/T19MiniCraftTest.unity",
        };

        [MenuItem("Tools/PastryWorld/修复测试场景 AudioListener")]
        public static void Fix()
        {
            int fixedScenes = 0;
            foreach (var path in Scenes)
            {
                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);

                if (Object.FindObjectsOfType<AudioListener>(true).Length > 0)
                {
                    Debug.Log($"[TestSceneAudioFixer] {path} 已有 AudioListener，跳过");
                    continue;
                }

                var cameras = Object.FindObjectsOfType<Camera>(true);
                if (cameras.Length == 0)
                {
                    Debug.LogWarning($"[TestSceneAudioFixer] {path} 无相机，跳过");
                    continue;
                }

                cameras[0].gameObject.AddComponent<AudioListener>();
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                fixedScenes++;
                Debug.Log($"[TestSceneAudioFixer] {path} → 相机 {cameras[0].name} 添加 AudioListener");
            }

            Debug.Log($"[TestSceneAudioFixer] 完成：{fixedScenes}/{Scenes.Length} 个场景修复。请回到场景后用 git 提交场景变更");
        }
    }
}
