using UnityEngine;
using UnityEngine.SceneManagement;

namespace PastryWorld.Core
{
    /// <summary>
    /// 启动场景加载器。加载存档后跳转主场景。
    /// </summary>
    public class BootstrapLoader : MonoBehaviour
    {
        [SerializeField] private string _mainSceneName = "Main";

        void Start()
        {
            // TODO(T15): 加载存档
            // TODO: 初始化事件总线、存档系统等

            SceneManager.LoadScene(_mainSceneName);
        }
    }
}
