#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace PastryWorld.Editor
{
    public static class BuildUtility
    {
        /// <summary>
        /// 仅编译检查，不生成完整构建。
        /// </summary>
        public static void CompileCheck()
        {
            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = new string[] { "Assets/Scenes/Bootstrap.unity" },
                locationPathName = "Builds/CompileCheck",
                target = BuildTarget.StandaloneLinux64,
                options = BuildOptions.None
            };

            var report = BuildPipeline.BuildPlayer(options);

            if (report.summary.result == BuildResult.Succeeded)
            {
                Debug.Log("编译检查通过");
            }
            else
            {
                Debug.LogError($"编译失败: {report.summary.result}");
                System.Environment.Exit(1);
            }
        }
    }
}
#endif
