using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using PastryWorld.Craft;

namespace PastryWorld.Craft.Editor
{
    /// <summary>
    /// 品质调参工具。可视化 Fréchet Distance 和品质分数。
    /// 技术预判报告 T4。
    /// 菜单：Tools/PastryWorld/Quality Tuning Window
    /// </summary>
    public class QualityTuningWindow : EditorWindow
    {
        [MenuItem("Tools/PastryWorld/Quality Tuning Window")]
        public static void ShowWindow()
        {
            var window = GetWindow<QualityTuningWindow>("品质调参工具");
            window.minSize = new Vector2(600, 500);
        }

        private List<Vector2> _playerTrajectory = new();
        private List<Vector2> _idealTrajectory = new();
        private float _maxAllowableDistance = 100f;
        private float _lastDistance;
        private float _lastScore;
        private bool _normalized = false;

        // 模拟轨迹生成参数
        private int _trajectorySamples = 32;
        private float _playerNoise = 15f;
        private float _playerScale = 0.9f;
        private float _playerRotation = 5f;

        void OnGUI()
        {
            GUILayout.Label("品质调参工具", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "生成模拟轨迹（理想圆 vs 带噪声圆），用 Fréchet Distance 评估品质。\n" +
                "调整参数观察分数变化，确定合理的 maxAllowableDistance 值。",
                MessageType.Info);

            EditorGUILayout.Space();

            // 参数
            _maxAllowableDistance = EditorGUILayout.FloatField("最大允许距离", _maxAllowableDistance);
            _normalized = EditorGUILayout.Toggle("归一化模式", _normalized);

            EditorGUILayout.Space();
            GUILayout.Label("模拟轨迹参数", EditorStyles.boldLabel);
            _trajectorySamples = EditorGUILayout.IntSlider("采样数", _trajectorySamples, 8, 128);
            _playerNoise = EditorGUILayout.Slider("玩家噪声", _playerNoise, 0f, 50f);
            _playerScale = EditorGUILayout.Slider("玩家缩放", _playerScale, 0.5f, 1.5f);
            _playerRotation = EditorGUILayout.Slider("玩家旋转(度)", _playerRotation, -30f, 30f);

            EditorGUILayout.Space();

            if (GUILayout.Button("生成轨迹并评估", GUILayout.Height(30)))
            {
                GenerateTrajectories();
                EvaluateTrajectories();
            }

            // 结果显示
            EditorGUILayout.Space();
            GUILayout.Label("评估结果", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Fréchet Distance", _lastDistance.ToString("F2"));
            EditorGUILayout.LabelField("品质分数", _lastScore.ToString("F3"));

            // 分数条
            EditorGUI.DrawRect(
                GUILayoutUtility.GetRect(0, 20),
                Color.Lerp(Color.red, Color.green, _lastScore));
            EditorGUI.LabelField(GUILayoutUtility.GetRect(0, 20), $"  {_lastScore:F3}",
                new GUIStyle(EditorStyles.label) { fontStyle = FontStyle.Bold });

            // 绘制轨迹
            EditorGUILayout.Space();
            GUILayout.Label("轨迹可视化", EditorStyles.boldLabel);
            Rect drawRect = GUILayoutUtility.GetRect(300, 300);
            DrawTrajectories(drawRect);

            // 复制参数到 QualityEvaluator
            EditorGUILayout.Space();
            if (GUILayout.Button("复制参数到选中物体的 QualityEvaluator"))
            {
                var evaluator = Selection.activeGameObject?.GetComponent<QualityEvaluator>();
                if (evaluator != null)
                {
                    var so = new SerializedObject(evaluator);
                    so.FindProperty("_maxAllowableDistance").floatValue = _maxAllowableDistance;
                    so.ApplyModifiedProperties();
                    Debug.Log($"已设置 maxAllowableDistance = {_maxAllowableDistance}");
                }
                else
                {
                    ShowNotification(new GUIContent("请选中带 QualityEvaluator 的物体"));
                }
            }
        }

        private void GenerateTrajectories()
        {
            _idealTrajectory.Clear();
            _playerTrajectory.Clear();

            float radius = 100f;
            for (int i = 0; i < _trajectorySamples; i++)
            {
                float t = (float)i / (_trajectorySamples - 1);
                float angle = t * Mathf.PI * 2f;

                // 理想轨迹：标准圆
                Vector2 ideal = new Vector2(
                    Mathf.Cos(angle) * radius,
                    Mathf.Sin(angle) * radius);
                _idealTrajectory.Add(ideal);

                // 玩家轨迹：带噪声+缩放+旋转的圆
                float rad = _playerRotation * Mathf.Deg2Rad;
                Vector2 rotated = new Vector2(
                    Mathf.Cos(angle + rad) * radius * _playerScale,
                    Mathf.Sin(angle + rad) * radius * _playerScale);
                Vector2 noise = new Vector2(
                    Random.Range(-_playerNoise, _playerNoise),
                    Random.Range(-_playerNoise, _playerNoise));
                _playerTrajectory.Add(rotated + noise);
            }
        }

        private void EvaluateTrajectories()
        {
            if (_idealTrajectory.Count == 0 || _playerTrajectory.Count == 0)
            {
                GenerateTrajectories();
            }

            _lastDistance = FrechetDistance.CalculateFast(
                _playerTrajectory, _idealTrajectory, 64);
            _lastScore = FrechetDistance.DistanceToScore(_lastDistance, _maxAllowableDistance);
        }

        private void DrawTrajectories(Rect rect)
        {
            // 背景
            EditorGUI.DrawRect(rect, new Color(0.15f, 0.15f, 0.15f));

            if (_idealTrajectory.Count == 0) return;

            // 计算缩放：轨迹坐标 → rect 坐标
            Vector2 center = rect.center;
            float scale = Mathf.Min(rect.width, rect.height) / 300f;

            // 绘制理想轨迹（绿色）
            DrawPolyline(_idealTrajectory, center, scale, new Color(0.3f, 1f, 0.3f));

            // 绘制玩家轨迹（黄色）
            DrawPolyline(_playerTrajectory, center, scale, new Color(1f, 0.9f, 0.3f));

            // 绘制点
            foreach (var p in _idealTrajectory)
            {
                Vector2 pos = center + p * scale;
                EditorGUI.DrawRect(new Rect(pos.x - 2, pos.y - 2, 4, 4), new Color(0.3f, 1f, 0.3f));
            }
            foreach (var p in _playerTrajectory)
            {
                Vector2 pos = center + p * scale;
                EditorGUI.DrawRect(new Rect(pos.x - 2, pos.y - 2, 4, 4), new Color(1f, 0.9f, 0.3f));
            }
        }

        private void DrawPolyline(List<Vector2> points, Vector2 center, float scale, Color color)
        {
            if (points.Count < 2) return;

            for (int i = 0; i < points.Count - 1; i++)
            {
                Vector2 a = center + points[i] * scale;
                Vector2 b = center + points[i + 1] * scale;
                DrawLine(a, b, color);
            }
        }

        private void DrawLine(Vector2 a, Vector2 b, Color color)
        {
            Color old = GUI.color;
            GUI.color = color;
            Handles.BeginGUI();
            Handles.DrawAAPolyLine(3f, a, b);
            Handles.EndGUI();
            GUI.color = old;
        }
    }
}
