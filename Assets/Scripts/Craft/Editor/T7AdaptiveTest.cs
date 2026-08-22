using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using PastryWorld.Core;

namespace PastryWorld.Craft.Editor
{
    /// <summary>
    /// T7 失败自适应测试。
    /// 编辑模式下运行断言：容错倍率增长、上限、完美档锁定、引导模式开关、事件发布。
    /// 菜单：Tools/PastryWorld/T7 失败自适应测试
    /// </summary>
    public static class T7AdaptiveTest
    {
        private static int _passed;
        private static int _failed;

        [MenuItem("Tools/PastryWorld/T7 失败自适应测试")]
        public static void RunTests()
        {
            _passed = 0;
            _failed = 0;

            var adaptiveGo = new GameObject("AdaptiveTest");
            var evalGo = new GameObject("EvalTest");
            try
            {
                var adaptive = adaptiveGo.AddComponent<AdaptiveDifficulty>();
                var evaluator = evalGo.AddComponent<QualityEvaluator>();
                evaluator.SetAdaptive(adaptive);

                // --- 事件捕获 ---
                var received = new List<AdaptiveGuidanceEvent>();
                EventBus.Default.Subscribe<AdaptiveGuidanceEvent>(e => received.Add(e));

                // --- 1. 初始状态 ---
                Assert(adaptive.ConsecutiveFailures == 0, "初始失败计数为 0");
                Assert(Mathf.Approximately(adaptive.CurrentToleranceMultiplier, 1f), "初始容错倍率 1.0");
                Assert(!adaptive.IsGuidanceActive, "初始引导模式关闭");

                // --- 2. 无失败时分数不变（diff=5, tol=10 → 0.5）---
                float baseScore = evaluator.Evaluate(105f, 100f, 10f);
                Assert(Mathf.Approximately(baseScore, 0.5f), $"无失败分数 0.5（实际 {baseScore:F3}）");

                // --- 3. 连续失败 3 次：倍率 1.3，分数提升但不进完美档 ---
                for (int i = 0; i < 3; i++) adaptive.OnStepResult(0.3f);
                Assert(Mathf.Approximately(adaptive.CurrentToleranceMultiplier, 1.3f),
                    $"3 次失败后倍率 1.3（实际 {adaptive.CurrentToleranceMultiplier:F2}）");
                float boosted = evaluator.Evaluate(105f, 100f, 10f); // 1 - 5/13 ≈ 0.615
                Assert(boosted > 0.6f && boosted < 0.9f,
                    $"容错扩大提分至良好档（实际 {boosted:F3}）");

                // --- 4. 完美档锁定：接近命中（diff=0.5）不因容错扩大超过 0.9 ---
                float nearPerfect = evaluator.Evaluate(100.5f, 100f, 10f); // 基础 0.95
                Assert(Mathf.Approximately(nearPerfect, 0.9f),
                    $"接近命中提分封顶 0.9（实际 {nearPerfect:F3}）");

                // --- 5. 完全命中不被压低：diff=0 基础分 1.0，保持 1.0 ---
                float exact = evaluator.Evaluate(100f, 100f, 10f);
                Assert(Mathf.Approximately(exact, 1f),
                    $"完全命中保持 1.0 不被压低（实际 {exact:F3}）");

                // --- 6. 第 4 次失败：引导模式激活 + 事件发布 ---
                adaptive.OnStepResult(0.2f);
                Assert(adaptive.IsGuidanceActive, "4 次失败后引导模式激活");
                Assert(received.Count == 1 && received[0].active,
                    "引导激活事件已发布");
                Assert(received[0].consecutiveFailures == 4, "事件携带失败计数 4");

                // --- 7. 成功一次：失败数递减到 3，引导解除 ---
                adaptive.OnStepResult(0.8f);
                Assert(adaptive.ConsecutiveFailures == 3, "成功后失败计数递减为 3");
                Assert(!adaptive.IsGuidanceActive, "成功后引导模式解除");
                Assert(received.Count == 2 && !received[1].active, "引导解除事件已发布");

                // --- 8. 倍率上限 1.5 ---
                for (int i = 0; i < 10; i++) adaptive.OnStepResult(0f);
                Assert(Mathf.Approximately(adaptive.CurrentToleranceMultiplier, 1.5f),
                    $"倍率封顶 1.5（实际 {adaptive.CurrentToleranceMultiplier:F2}）");

                // --- 9. ResetState ---
                adaptive.ResetState();
                Assert(adaptive.ConsecutiveFailures == 0 && !adaptive.IsGuidanceActive,
                    "ResetState 清零并解除引导");
            }
            finally
            {
                Object.DestroyImmediate(adaptiveGo);
                Object.DestroyImmediate(evalGo);
                EventBus.ResetDefault(); // 清理测试订阅
            }

            Debug.Log($"[T7 测试] 通过 {_passed} / {_passed + _failed}");
            if (_failed > 0)
                Debug.LogError($"[T7 测试] 失败 {_failed} 项！");
            else
                Debug.Log("[T7 测试] 全部通过 ✓");
        }

        private static void Assert(bool condition, string message)
        {
            if (condition) { _passed++; Debug.Log($"  ✓ {message}"); }
            else { _failed++; Debug.LogError($"  ✗ {message}"); }
        }
    }
}
