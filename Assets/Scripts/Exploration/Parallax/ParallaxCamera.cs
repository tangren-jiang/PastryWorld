using UnityEngine;

namespace PastryWorld.Exploration
{
    /// <summary>
    /// 相机平滑跟随。T11 视差系统需要相机移动才有视差效果。
    /// 挂在主相机上，LateUpdate 平滑追踪玩家。
    /// </summary>
    public class ParallaxCamera : MonoBehaviour
    {
        [Tooltip("跟随目标（玩家）。为空时自动查找 PlayerController")]
        [SerializeField] private Transform _target;

        [Tooltip("跟随平滑度（0=瞬间，越大越平滑）")]
        [SerializeField] private float _smoothTime = 0.15f;

        [Tooltip("Z 轴偏移（保持相机与 2D 平面的距离）")]
        [SerializeField] private float _zOffset = -10f;

        private Vector3 _velocity;

        void Start()
        {
            if (_target == null)
            {
                var player = FindFirstObjectByType<PlayerController>();
                if (player != null) _target = player.transform;
            }

            if (_target != null)
            {
                transform.position = new Vector3(
                    _target.position.x, _target.position.y, _zOffset);
            }
        }

        void LateUpdate()
        {
            if (_target != null)
            {
                Vector3 targetPos = new Vector3(
                    _target.position.x, _target.position.y, _zOffset);
                transform.position = Vector3.SmoothDamp(
                    transform.position, targetPos, ref _velocity, _smoothTime);
            }
        }
    }
}
