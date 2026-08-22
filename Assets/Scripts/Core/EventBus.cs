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
    /// 事件总线实现。基于类型安全的泛型订阅。
    /// </summary>
    public class EventBus : IEventBus
    {
        private readonly Dictionary<Type, object> _handlers = new();

        /// <summary>
        /// 全局共享实例。所有模块使用同一实例实现跨模块通信。
        /// 场景切换时调用 ResetDefault() 清理。
        /// </summary>
        private static EventBus _default;
        public static EventBus Default
        {
            get { return _default ??= new EventBus(); }
        }

        /// <summary>
        /// 清理全局实例（场景切换时调用）。
        /// </summary>
        public static void ResetDefault() { _default = null; }

        public void Subscribe<TEvent>(Action<TEvent> handler) where TEvent : struct
        {
            if (handler == null) return;
            var type = typeof(TEvent);
            if (!_handlers.TryGetValue(type, out var list))
            {
                list = new List<Action<TEvent>>();
                _handlers[type] = list;
            }
            ((List<Action<TEvent>>)list).Add(handler);
        }

        public void Unsubscribe<TEvent>(Action<TEvent> handler) where TEvent : struct
        {
            if (handler == null) return;
            var type = typeof(TEvent);
            if (_handlers.TryGetValue(type, out var list))
            {
                ((List<Action<TEvent>>)list).Remove(handler);
            }
        }

        public void Publish<TEvent>(TEvent evt) where TEvent : struct
        {
            var type = typeof(TEvent);
            if (_handlers.TryGetValue(type, out var list))
            {
                var handlers = (List<Action<TEvent>>)list;
                // 复制一份避免迭代中修改
                var copy = handlers.ToArray();
                foreach (var handler in copy)
                {
                    handler?.Invoke(evt);
                }
            }
        }

        public void Clear()
        {
            _handlers.Clear();
        }
    }
}
