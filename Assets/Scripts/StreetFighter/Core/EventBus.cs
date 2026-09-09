using System;
using System.Collections.Generic;

namespace StreetFighter.Core
{
    /// <summary>
    /// 极简字符串事件总线。
    /// 同名事件可以有多个监听，触发顺序是「后注册的先执行」。
    /// </summary>
    public sealed class EventBus
    {
        private readonly Dictionary<string, List<Action>> _listeners = new Dictionary<string, List<Action>>();

        /// <summary>注册监听。</summary>
        /// <param name="key">事件名，见 <see cref="GameEvents"/>。</param>
        /// <param name="callback">回调。</param>
        /// <param name="replace">为 true 时先清空该事件的旧监听。</param>
        public void AddListener(string key, Action callback, bool replace = false)
        {
            if (string.IsNullOrEmpty(key) || callback == null)
            {
                return;
            }

            List<Action> listeners;
            if (!_listeners.TryGetValue(key, out listeners))
            {
                listeners = new List<Action>();
                _listeners[key] = listeners;
            }

            if (replace)
            {
                listeners.Clear();
            }

            listeners.Add(callback);
        }

        /// <summary>移除某个事件上的全部监听。</summary>
        public void RemoveListener(string key)
        {
            List<Action> listeners;
            if (_listeners.TryGetValue(key, out listeners))
            {
                listeners.Clear();
            }
        }

        /// <summary>触发事件。</summary>
        public void Invoke(string key)
        {
            List<Action> listeners;
            if (!_listeners.TryGetValue(key, out listeners))
            {
                return;
            }

            for (int i = listeners.Count - 1; i >= 0; i--)
            {
                listeners[i]?.Invoke();
            }
        }

        /// <summary>清空所有监听。</summary>
        public void Clear() => _listeners.Clear();
    }
}
