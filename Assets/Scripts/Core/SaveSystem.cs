using System;

namespace PastryWorld.Core
{
    /// <summary>
    /// 存档系统接口。
    /// </summary>
    public interface ISaveSystem
    {
        void Save<T>(string key, T data);
        T Load<T>(string key);
        bool HasKey(string key);
        void DeleteKey(string key);
    }

    /// <summary>
    /// 存档系统空实现。实际逻辑在 T15 开发周完成。
    /// </summary>
    public class SaveSystem : ISaveSystem
    {
        public void Save<T>(string key, T data)
        {
            // TODO(T15): 实现存档逻辑
        }

        public T Load<T>(string key)
        {
            // TODO(T15): 实现读档逻辑
            return default;
        }

        public bool HasKey(string key)
        {
            // TODO(T15): 实现键检查
            return false;
        }

        public void DeleteKey(string key)
        {
            // TODO(T15): 实现删除键
        }
    }
}
