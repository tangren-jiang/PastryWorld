using UnityEngine;
using UnityEditor;
using System.Reflection;

namespace PastryWorld.Exploration.Editor
{
    /// <summary>
    /// 配置碰撞层和碰撞矩阵。运行一次即可。
    /// 技术预判报告 T9。
    /// </summary>
    public static class CollisionLayerSetup
    {
        private const int LAYER_CHARACTER = 8;
        private const int LAYER_ENVIRONMENT = 9;
        private const int LAYER_INTERACTABLE = 10;
        private const int LAYER_TRIGGER = 11;
        private const int LAYER_BOUNDARY = 12;

        [MenuItem("Tools/PastryWorld/Setup Collision Layers")]
        public static void Setup()
        {
            var tagManager = GetTagManager();
            if (tagManager == null)
            {
                Debug.LogError("无法获取 TagManager");
                return;
            }

            var layersProp = tagManager.FindProperty("layers");
            if (layersProp == null || !layersProp.isArray || layersProp.arraySize < 13)
            {
                Debug.LogError("layers 数组异常");
                return;
            }

            // 设置层名
            SetLayerName(layersProp, LAYER_CHARACTER, "Character");
            SetLayerName(layersProp, LAYER_ENVIRONMENT, "Environment");
            SetLayerName(layersProp, LAYER_INTERACTABLE, "Interactable");
            SetLayerName(layersProp, LAYER_TRIGGER, "Trigger");
            SetLayerName(layersProp, LAYER_BOUNDARY, "Boundary");

            // 碰撞矩阵：
            // Character x Environment = 碰（物理阻挡）
            // Character x Interactable = 碰（但 Interactable 是 Trigger）
            // Character x Trigger = 不碰（Trigger 只做检测）
            // Character x Boundary = 碰（物理边界）
            // Interactable x Environment = 不碰
            // Trigger x 一切 = 不碰（纯检测）
            // Boundary x 一切 = 碰（阻挡一切）
            //
            // 实际用 layer Collision Matrix:
            // 勾选 = 碰撞（产生接触），不勾 = 忽略
            // Character-Environment: true
            // Character-Interactable: true (用 Collider2D.isTrigger=true)
            // Character-Trigger: false
            // Character-Boundary: true
            // Interactable-Environment: false
            // Interactable-Trigger: false
            // Interactable-Boundary: true
            // Trigger-一切: false
            // Boundary-一切: true (except Boundary-Boundary)
            SetCollision(LAYER_CHARACTER, LAYER_ENVIRONMENT, true);
            SetCollision(LAYER_CHARACTER, LAYER_INTERACTABLE, true);
            SetCollision(LAYER_CHARACTER, LAYER_TRIGGER, false);
            SetCollision(LAYER_CHARACTER, LAYER_BOUNDARY, true);
            SetCollision(LAYER_INTERACTABLE, LAYER_ENVIRONMENT, false);
            SetCollision(LAYER_INTERACTABLE, LAYER_TRIGGER, false);
            SetCollision(LAYER_INTERACTABLE, LAYER_BOUNDARY, true);
            SetCollision(LAYER_TRIGGER, LAYER_TRIGGER, false);
            SetCollision(LAYER_TRIGGER, LAYER_BOUNDARY, false);
            SetCollision(LAYER_BOUNDARY, LAYER_BOUNDARY, false);

            tagManager.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();

            Debug.Log("碰撞层设置完成: Character(8), Environment(9), Interactable(10), Trigger(11), Boundary(12)");
        }

        private static SerializedObject GetTagManager()
        {
            var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset"));
            return tagManager;
        }

        private static void SetLayerName(SerializedProperty layersProp, int index, string name)
        {
            var element = layersProp.GetArrayElementAtIndex(index);
            if (element.stringValue != name)
            {
                element.stringValue = name;
            }
        }

        private static void SetCollision(int layerA, int layerB, bool shouldCollide)
        {
            // Physics2D 碰撞矩阵是通过 TagManager 的 m_LayerCollisionMatrix 设置的
            // 这是一个 32x32 的位掩码，存储为一维数组
            var tagManager = GetTagManager();
            if (tagManager == null) return;

            var matrixProp = tagManager.FindProperty("m_LayerCollisionMatrix");
            if (matrixProp == null || !matrixProp.isArray)
            {
                // 如果没有矩阵属性，使用 Physics2D API
                Physics2D.IgnoreLayerCollision(layerA, layerB, !shouldCollide);
                return;
            }

            // 位掩码方式：每个 layer 有一个 int，对应位表示是否与该层碰撞
            int indexA = layerA;
            int indexB = layerB;
            int maskA = matrixProp.GetArrayElementAtIndex(indexA).intValue;
            int maskB = matrixProp.GetArrayElementAtIndex(indexB).intValue;

            if (shouldCollide)
            {
                maskA |= (1 << layerB);
                maskB |= (1 << layerA);
            }
            else
            {
                maskA &= ~(1 << layerB);
                maskB &= ~(1 << layerA);
            }

            matrixProp.GetArrayElementAtIndex(indexA).intValue = maskA;
            matrixProp.GetArrayElementAtIndex(indexB).intValue = maskB;

            tagManager.ApplyModifiedProperties();
        }
    }
}
