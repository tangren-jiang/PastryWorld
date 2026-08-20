using UnityEngine;

namespace PastryWorld.Craft
{
    /// <summary>
    /// 制作步骤抽象基类。技术预判报告 T1。
    /// </summary>
    public abstract class CraftStep : MonoBehaviour
    {
        [SerializeField] protected string _stepId;
        [SerializeField] protected bool _isMiniStep = false;

        public string StepId => _stepId;
        public bool IsMiniStep => _isMiniStep;

        /// <summary>步骤开始</summary>
        public abstract void OnStepBegin();

        /// <summary>步骤结束（成功或失败）</summary>
        public abstract void OnStepEnd(bool success);
    }
}
