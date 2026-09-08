using System;
using System.Collections;
using System.Collections.Generic;

namespace StreetFighter
{
    /// <summary>复刻 interface.js 的 Event：字符串键 + 回调列表。type=1 时先清空旧监听。</summary>
    public class Evt
    {
        private readonly Dictionary<string, List<Action>> _map = new Dictionary<string, List<Action>>();

        public void Listen(string key, Action fn, int type = 0)
        {
            List<Action> list;
            if (!_map.TryGetValue(key, out list))
            {
                list = new List<Action>();
                _map[key] = list;
            }
            if (type == 1) list.Clear();
            list.Add(fn);
        }

        public void RemoveListen(string key)
        {
            List<Action> list;
            if (_map.TryGetValue(key, out list)) list.Clear();
        }

        public void Fire(string key)
        {
            List<Action> list;
            if (!_map.TryGetValue(key, out list)) return;
            for (int i = list.Count - 1; i >= 0; i--) list[i]?.Invoke();
        }
    }

    /// <summary>复刻 interface.js 的 Queue。</summary>
    public class Q<T>
    {
        private readonly List<T> _stack = new List<T>();

        public void Add(T obj) => _stack.Add(obj);

        public void AddRange(IEnumerable<T> objs)
        {
            foreach (var o in objs) _stack.Add(o);
        }

        public void Unshift(T obj) => _stack.Insert(0, obj);

        public T Dequeue()
        {
            if (_stack.Count == 0) return default(T);
            T v = _stack[0];
            _stack.RemoveAt(0);
            return v;
        }

        public void Clean() => _stack.Clear();

        public bool IsEmpty => _stack.Count == 0;

        public T Last => _stack.Count == 0 ? default(T) : _stack[_stack.Count - 1];

        public List<T> Get() => _stack;
    }

    /// <summary>复刻 interface.js 的 Lock：数字级别锁，&gt;0 表示锁定。</summary>
    public class Lock
    {
        private int _level;
        public void Set(int level) => _level = level;
        public int Level => _level;
        public bool Locked => _level > 0;
    }
}
