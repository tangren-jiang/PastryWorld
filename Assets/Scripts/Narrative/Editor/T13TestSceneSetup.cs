using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using PastryWorld.Narrative;

namespace PastryWorld.Narrative.Editor
{
    /// <summary>
    /// T13 节拍状态机测试场景构建脚本。
    /// 运行：Unity batchmode -executeMethod PastryWorld.Narrative.Editor.T13TestSceneSetup.Setup
    /// </summary>
    public static class T13TestSceneSetup
    {
        [MenuItem("Tools/PastryWorld/T13 Beat Test Scene Setup")]
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

            // 创建节拍 SO 资产（K1-K5 简化版）
            var beats = new BeatSO[5];
            string[] ids = { "K1", "K2", "K3", "K4", "K5" };
            string[] texts = {
                "蜂蜜婆婆：来，孩子，尝尝这蜂蜜涂果干。",
                "蜂蜜婆婆：这蜂蜜，是我年轻时采的花蜜。",
                "【记忆闪回——金色花海】",
                "蜂蜜婆婆：那时候，一切都还是甜的。",
                "【K5 选项】你想起什么了吗？"
            };
            BeatType[] types = {
                BeatType.Dialogue,
                BeatType.Dialogue,
                BeatType.Memory,
                BeatType.Dialogue,
                BeatType.Choice
            };

            for (int i = 0; i < 5; i++)
            {
                beats[i] = ScriptableObject.CreateInstance<BeatSO>();
                beats[i].beatId = ids[i];
                beats[i].beatType = types[i];
                beats[i].dialogueText = texts[i];
                beats[i].nextBeatId = i < 4 ? ids[i + 1] : "";
            }

            // BeatManager
            var beatMgrObj = new GameObject("BeatManager");
            var beatMgr = beatMgrObj.AddComponent<BeatManager>();

            // 用 SerializedObject 设置 private 字段
            var so = new SerializedObject(beatMgr);
            so.FindProperty("_beats").arraySize = 5;
            for (int i = 0; i < 5; i++)
            {
                so.FindProperty("_beats").GetArrayElementAtIndex(i).objectReferenceValue = beats[i];
            }
            so.FindProperty("_startBeatId").stringValue = "K1";
            so.ApplyModifiedProperties();

            // 创建一个简单的 BeatTestController 来监听事件并打印
            // 用内联组件不方便，直接用 MonoBehaviour 反射或创建脚本
            // 这里简单加一个空 GameObject 标记
            var testObj = new GameObject("BeatTestListener");
            testObj.AddComponent<BeatTestListener>();

            // 保存场景
            if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
                AssetDatabase.CreateFolder("Assets", "Scenes");

            // 保存 SO 资产
            if (!AssetDatabase.IsValidFolder("Assets/Settings/Beats"))
                AssetDatabase.CreateFolder("Assets/Settings", "Beats");

            for (int i = 0; i < 5; i++)
            {
                string path = $"Assets/Settings/Beats/BeatSO_{ids[i]}.asset";
                AssetDatabase.CreateAsset(beats[i], path);
            }

            string scenePath = "Assets/Scenes/BeatTest.unity";
            EditorSceneManager.SaveScene(scene, scenePath);

            AssetDatabase.SaveAssets();
            Debug.Log("T13 节拍测试场景创建完成: " + scenePath);
            Debug.Log("5 个节拍: K1→K2→K3→K4→K5（Dialogue, Dialogue, Memory, Dialogue, Choice）");
        }
    }

    /// <summary>
    /// 测试监听器。订阅 BeatManager 事件并打印。
    /// </summary>
    public class BeatTestListener : MonoBehaviour
    {
        void Start()
        {
            var beatMgr = FindObjectOfType<BeatManager>();
            if (beatMgr == null)
            {
                Debug.LogError("[BeatTestListener] 未找到 BeatManager");
                return;
            }

            // 启动节拍链
            beatMgr.StartChain();

            // 监听事件（需要通过 EventBus，但 BeatManager 内部创建了 EventBus）
            // 这里简单通过 StartChain 后检查 CurrentBeat
            Debug.Log($"[BeatTestListener] 当前节拍: {beatMgr.CurrentBeatId} ({beatMgr.CurrentBeat?.beatType})");

            // 测试前进
            beatMgr.Advance();
            Debug.Log($"[BeatTestListener] 前进后节拍: {beatMgr.CurrentBeatId}");

            beatMgr.Advance();
            Debug.Log($"[BeatTestListener] 再前进: {beatMgr.CurrentBeatId} ({beatMgr.CurrentBeat?.beatType})");

            beatMgr.Advance();
            beatMgr.Advance();
            Debug.Log($"[BeatTestListener] 到末尾，IsRunning={beatMgr.IsRunning}");
        }
    }
}
