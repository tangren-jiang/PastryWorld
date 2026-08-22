using System.Collections.Generic;
using UnityEngine;

namespace PastryWorld.Craft.Dough
{
    /// <summary>
    /// 面团网格形变器。生成程序化网格 + 骨骼控制点 + 弹簧回弹。
    /// 技术预判报告 T3：SpriteSkin + 自定义 Mesh 变形。
    /// 不依赖 SpriteSkin 编辑器数据，运行时生成网格和骨骼权重。
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class DoughMeshDeformer : MonoBehaviour
    {
        [Header("配置")]
        [SerializeField] private DoughPhysicsConfig _config;

        [Header("骨骼控制点")]
        [Tooltip("中心骨骼 + 环形排列的边缘骨骼")]
        [Range(3, 12)] [SerializeField] private int _edgeBoneCount = 8;

        private Mesh _mesh;
        private MeshFilter _meshFilter;
        private MeshRenderer _meshRenderer;

        // 原始顶点位置（静止状态）
        private Vector3[] _baseVertices;

        // 顶点 → 影响它的骨骼索引和权重
        private struct BoneWeightData
        {
            public int boneIndex0;
            public int boneIndex1;
            public float weight0;
            public float weight1;
        }
        private BoneWeightData[] _vertexWeights;

        // 骨骼列表
        private List<SpringBone> _bones = new List<SpringBone>();

        // 当前揉面圆心（世界坐标）
        private Vector2 _kneadCenter;
        private bool _hasKneadCenter;

        void Awake()
        {
            _meshFilter = GetComponent<MeshFilter>();
            _meshRenderer = GetComponent<MeshRenderer>();
            GenerateMesh();
            CreateBones();
        }

        void Update()
        {
            // 更新所有骨骼的弹簧物理
            foreach (var bone in _bones)
            {
                bone.UpdatePhysics(Time.deltaTime);
            }

            // 根据骨骼位置更新网格顶点
            DeformMesh();
        }

        /// <summary>
        /// 程序化生成圆形面团网格。
        /// </summary>
        private void GenerateMesh()
        {
            int res = _config != null ? _config.meshResolution : 12;
            float radius = _config != null ? _config.doughRadius : 2f;

            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            var uvs = new List<Vector2>();

            // 中心顶点
            vertices.Add(Vector3.zero);
            uvs.Add(new Vector2(0.5f, 0.5f));

            // 环形顶点
            int rings = Mathf.Max(2, res / 3);
            for (int ring = 1; ring <= rings; ring++)
            {
                float ringRadius = radius * ring / rings;
                int segments = res;
                for (int i = 0; i < segments; i++)
                {
                    float angle = (float)i / segments * Mathf.PI * 2f;
                    vertices.Add(new Vector3(
                        Mathf.Cos(angle) * ringRadius,
                        Mathf.Sin(angle) * ringRadius,
                        0f
                    ));
                    uvs.Add(new Vector2(
                        0.5f + Mathf.Cos(angle) * 0.5f * (ring / (float)rings),
                        0.5f + Mathf.Sin(angle) * 0.5f * (ring / (float)rings)
                    ));
                }
            }

            // 构建三角形
            // 中心环
            for (int i = 1; i <= res; i++)
            {
                int next = (i % res) + 1;
                triangles.Add(0);
                triangles.Add(i);
                triangles.Add(next);
            }

            // 内环之间
            for (int ring = 0; ring < rings - 1; ring++)
            {
                int offset0 = 1 + ring * res;
                int offset1 = 1 + (ring + 1) * res;
                for (int i = 0; i < res; i++)
                {
                    int next = (i + 1) % res;
                    triangles.Add(offset0 + i);
                    triangles.Add(offset1 + i);
                    triangles.Add(offset0 + next);
                    triangles.Add(offset0 + next);
                    triangles.Add(offset1 + i);
                    triangles.Add(offset1 + next);
                }
            }

            _mesh = new Mesh { name = "DoughMesh" };
            _mesh.SetVertices(vertices);
            _mesh.SetTriangles(triangles, 0);
            _mesh.SetUVs(0, uvs);
            _mesh.RecalculateNormals();
            _mesh.RecalculateBounds();

            _baseVertices = vertices.ToArray();
            _meshFilter.mesh = _mesh;
        }

        /// <summary>
        /// 创建骨骼控制点（中心 + 边缘环形排列）。
        /// </summary>
        private void CreateBones()
        {
            _bones.Clear();

            float stiffness = _config != null ? _config.stiffness : 50f;
            float damping = _config != null ? _config.damping : 12f;
            float maxDisp = _config != null ? _config.maxDisplacement : 0.8f;
            float radius = _config != null ? _config.doughRadius : 2f;

            // 中心骨骼
            _bones.Add(new SpringBone(Vector2.zero, stiffness, damping, maxDisp));

            // 边缘骨骼（环形排列）
            for (int i = 0; i < _edgeBoneCount; i++)
            {
                float angle = (float)i / _edgeBoneCount * Mathf.PI * 2f;
                var pos = new Vector2(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius);
                _bones.Add(new SpringBone(pos, stiffness, damping, maxDisp));
            }

            // 计算每个顶点的骨骼权重
            ComputeVertexWeights();
        }

        /// <summary>
        /// 计算每个顶点受哪些骨骼影响（距离最近的 2 个）。
        /// </summary>
        private void ComputeVertexWeights()
        {
            _vertexWeights = new BoneWeightData[_baseVertices.Length];

            for (int v = 0; v < _baseVertices.Length; v++)
            {
                var vertexPos = (Vector2)_baseVertices[v];

                // 找最近的 2 个骨骼
                int nearest0 = 0;
                float dist0 = float.MaxValue;
                int nearest1 = 1;
                float dist1 = float.MaxValue;

                for (int b = 0; b < _bones.Count; b++)
                {
                    float dist = (vertexPos - _bones[b].RestPosition).sqrMagnitude;
                    if (dist < dist0)
                    {
                        dist1 = dist0;
                        nearest1 = nearest0;
                        dist0 = dist;
                        nearest0 = b;
                    }
                    else if (dist < dist1)
                    {
                        dist1 = dist;
                        nearest1 = b;
                    }
                }

                // 基于距离的权重
                float total = dist0 + dist1;
                if (total < 0.0001f) total = 0.0001f;
                float w0 = dist1 / total;
                float w1 = dist0 / total;

                _vertexWeights[v] = new BoneWeightData
                {
                    boneIndex0 = nearest0,
                    boneIndex1 = nearest1,
                    weight0 = w0,
                    weight1 = w1
                };
            }
        }

        /// <summary>
        /// 根据骨骼当前位置变形网格顶点。
        /// </summary>
        private void DeformMesh()
        {
            var vertices = new Vector3[_baseVertices.Length];

            for (int v = 0; v < _baseVertices.Length; v++)
            {
                var restPos = (Vector2)_baseVertices[v];
                var w = _vertexWeights[v];

                Vector2 boneOffset0 = _bones[w.boneIndex0].CurrentPosition - _bones[w.boneIndex0].RestPosition;
                Vector2 boneOffset1 = _bones[w.boneIndex1].CurrentPosition - _bones[w.boneIndex1].RestPosition;

                Vector2 deformed = restPos + boneOffset0 * w.weight0 + boneOffset1 * w.weight1;
                vertices[v] = deformed;
            }

            _mesh.vertices = vertices;
            _mesh.RecalculateNormals();
            _mesh.RecalculateBounds();
        }

        /// <summary>
        /// 拖拽时推动附近的骨骼。
        /// </summary>
        /// <param name="worldPosition">拖拽位置（世界坐标）</param>
        /// <param name="deltaPosition">本帧位移</param>
        public void ApplyDrag(Vector2 worldPosition, Vector2 deltaPosition)
        {
            if (_config == null) return;

            float influenceRadius = _config.influenceRadius;
            float dragInfluence = _config.dragInfluence;
            float falloff = _config.influenceFalloff;

            // 记录揉面圆心
            if (!_hasKneadCenter)
            {
                _kneadCenter = worldPosition;
                _hasKneadCenter = true;
            }

            // 局部坐标转换
            Vector2 localPoint = WorldToLocal(worldPosition);

            foreach (var bone in _bones)
            {
                float dist = (bone.RestPosition - localPoint).magnitude;
                if (dist < influenceRadius)
                {
                    // 距离衰减权重
                    float t = 1f - (dist / influenceRadius);
                    t = Mathf.Pow(t, 1f + falloff * 3f);

                    // 转换 delta 到局部空间
                    Vector2 localDelta = WorldToLocalDelta(deltaPosition);
                    bone.ApplyDisplacement(localDelta * dragInfluence * t);
                }
            }
        }

        /// <summary>
        /// 重置所有骨骼到静止位置。
        /// </summary>
        public void ResetBones()
        {
            foreach (var bone in _bones)
            {
                bone.Reset();
            }
            _hasKneadCenter = false;
        }

        /// <summary>
        /// 获取当前形变量（0-1，0=静止，1=最大位移）。
        /// </summary>
        public float GetDeformationAmount()
        {
            float total = 0f;
            float maxDisp = _config != null ? _config.maxDisplacement : 0.8f;
            foreach (var bone in _bones)
            {
                total += bone.DisplacementMagnitude / maxDisp;
            }
            return Mathf.Clamp01(total / _bones.Count);
        }

        /// <summary>
        /// 世界坐标→局部坐标转换。
        /// </summary>
        private Vector2 WorldToLocal(Vector2 worldPos)
        {
            return transform.InverseTransformPoint(worldPos);
        }

        private Vector2 WorldToLocalDelta(Vector2 worldDelta)
        {
            return transform.InverseTransformDirection(worldDelta);
        }

        public int BoneCount => _bones.Count;
        public IReadOnlyList<SpringBone> Bones => _bones;
    }
}
