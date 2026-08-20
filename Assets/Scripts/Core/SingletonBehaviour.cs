using UnityEngine;

namespace PastryWorld.Core
{
    /// <summary>
    /// 单例基类。继承MonoBehaviour的模块入口使用。
    /// </summary>
    public abstract class SingletonBehaviour<T> : MonoBehaviour
        where T : SingletonBehaviour<T>
    {
        private static T _instance;

        public static T Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<T>();
                    if (_instance == null)
                    {
                        Debug.LogError($"[Singleton] {typeof(T).Name} 未在场景中找到！");
                    }
                }
                return _instance;
            }
        }

        protected virtual void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Debug.LogWarning($"[Singleton] {typeof(T).Name} 已存在，销毁重复实例");
                Destroy(gameObject);
                return;
            }
            _instance = (T)this;
        }
    }
}
