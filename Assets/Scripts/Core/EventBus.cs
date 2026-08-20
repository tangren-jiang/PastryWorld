using System;
using System.Collections.Generic;

namespace PastryWorld.Core
{
    /// <summary>
    /// 事件总线接口。模块间解耦通信。
    /// </summary>
    public interface IEventBus
    {
        void Subscribe<TEvent>(Action<TEvent> handler) where TEvent : struct;
        void Unsubscribe<TEvent>(Action<TEvent> handler) where TEvent : struct;
        void Publish<TEvent>(TEvent evt) where TEvent : struct;
    }

    /// <summary>
    /// 事件总线空实现。实际逻辑在 T4 开发周完成。
    /// </summary>
    public class EventBus : IEventBus
    {
        private readonly Dictionary<Type, object> _handlers = new();

        public void Subscribe<TEvent>(Action<TEvent> handler) where TEvent : struct
        {
            // TODO(T4): 实现订阅逻辑
        }

        public void Unsubscribe<TEvent>(Action<TEvent> handler) where TEvent : struct
        {
            // TODO(T4): 实现取消订阅逻辑
        }

        public void Publish<TEvent>(TEvent evt) where TEvent : struct
        {
            // TODO(T4): 实现发布逻辑
        }
    }
}
