using UnityEngine;

namespace PastryWorld.Craft.Dough
{
    /// <summary>
    /// 单个骨骼控制点的自定义阻尼弹簧物理。
    /// 不使用 Rigidbody2D，纯数学模拟。
    /// 技术预判报告 T3：自定义阻尼弹簧（非引擎刚体）。
    /// </summary>
    public class SpringBone
    {
        // 静止位置（原始位置）
        public Vector2 RestPosition { get; private set; }

        // 当前位置
        public Vector2 CurrentPosition { get; private set; }

        // 速度
        public Vector2 Velocity { get; private set; }

        // 弹簧参数（从 DoughPhysicsConfig 传入）
        private float _stiffness;
        private float _damping;
        private float _maxDisplacement;

        /// <summary>
        /// 初始化骨骼。
        /// </summary>
        public SpringBone(Vector2 restPosition, float stiffness, float damping, float maxDisplacement)
        {
            RestPosition = restPosition;
            CurrentPosition = restPosition;
            Velocity = Vector2.zero;
            _stiffness = stiffness;
            _damping = damping;
            _maxDisplacement = maxDisplacement;
        }

        /// <summary>
        /// 更新弹簧物理。在 Update/FixedUpdate 中调用。
        /// F = -k * (x - x_rest) - c * v  (Hooke + 阻尼)
        /// </summary>
        public void UpdatePhysics(float deltaTime)
        {
            Vector2 displacement = CurrentPosition - RestPosition;
            Vector2 springForce = -_stiffness * displacement;
            Vector2 dampingForce = -_damping * Velocity;
            Vector2 acceleration = springForce + dampingForce;

            Velocity += acceleration * deltaTime;
            CurrentPosition += Velocity * deltaTime;

            // 限制最大位移
            Vector2 offset = CurrentPosition - RestPosition;
            if (offset.magnitude > _maxDisplacement)
            {
                CurrentPosition = RestPosition + offset.normalized * _maxDisplacement;
                Velocity = Vector2.zero;
            }
        }

        /// <summary>
        /// 施加位移（拖拽时调用）。
        /// </summary>
        public void ApplyDisplacement(Vector2 deltaPosition)
        {
            Vector2 target = CurrentPosition + deltaPosition;
            Vector2 offset = target - RestPosition;
            if (offset.magnitude > _maxDisplacement)
            {
                target = RestPosition + offset.normalized * _maxDisplacement;
            }
            CurrentPosition = target;
        }

        /// <summary>
        /// 施加力（影响速度）。
        /// </summary>
        public void ApplyForce(Vector2 force)
        {
            Velocity += force;
        }

        /// <summary>
        /// 重置到静止位置。
        /// </summary>
        public void Reset()
        {
            CurrentPosition = RestPosition;
            Velocity = Vector2.zero;
        }

        /// <summary>
        /// 当前位移量（偏离静止位置的距离）。
        /// </summary>
        public float DisplacementMagnitude => (CurrentPosition - RestPosition).magnitude;

        /// <summary>
        /// 是否已接近静止（用于判断是否完成回弹）。
        /// </summary>
        public bool IsAtRest(float threshold = 0.01f)
        {
            return DisplacementMagnitude < threshold && Velocity.magnitude < threshold;
        }
    }
}
