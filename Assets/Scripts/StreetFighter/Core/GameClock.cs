using System;
using System.Collections.Generic;

namespace StreetFighter
{
    /// <summary>
    /// 复刻原版 timer.js：所有子系统共用一个固定节奏的帧驱动（17ms 一帧），
    /// 每帧从后往前执行回调；支持 push（插到队首，最先执行）/ unshift（插到队尾，最后执行）。
    /// 同时提供 setTimeout 等价的延时调用（使用游戏内时间，暂停时一起冻结）。
    /// </summary>
    public class GameClock
    {
        public const int TickMs = 17;

        public class Handle
        {
            public Action Fn;
            public int State; // 0 normal, 1 add, 2 stop
        }

        private struct Pending
        {
            public double Due;
            public Action Fn;
        }

        private readonly List<Handle> _timers = new List<Handle>();
        private readonly List<Handle> _prepare = new List<Handle>();
        private readonly List<Pending> _pending = new List<Pending>();

        /// <summary>游戏内累计时间（毫秒）。</summary>
        public double Now { get; private set; }

        public bool Running { get; set; }

        public Handle Add(Action fn) => new Handle { Fn = fn, State = 0 };

        public void Start(Handle h)
        {
            if (h == null) return;
            if (h.State == 0)
            {
                h.State = 1;
                _prepare.Insert(0, h);
            }
            else if (h.State == 2)
            {
                h.State = 1;
            }
        }

        public void Stop(Handle h)
        {
            if (h != null) h.State = 2;
        }

        /// <summary>push：直接放入队列首部（最先被执行，原版用于清屏）。</summary>
        public void Push(Handle h)
        {
            if (h == null) return;
            if (!_timers.Contains(h)) _timers.Insert(0, h);
            h.State = 1;
        }

        public void Timeout(Action fn, double ms)
        {
            _pending.Add(new Pending { Due = Now + ms, Fn = fn });
        }

        public void Tick()
        {
            Now += TickMs;

            for (int i = _pending.Count - 1; i >= 0; i--)
            {
                if (_pending[i].Due <= Now)
                {
                    var p = _pending[i];
                    _pending.RemoveAt(i);
                    p.Fn?.Invoke();
                }
            }

            for (int i = _prepare.Count - 1; i >= 0; i--) _timers.Insert(0, _prepare[i]);
            if (_prepare.Count > 0) _prepare.Clear();

            for (int i = _timers.Count - 1; i >= 0; i--)
            {
                var h = _timers[i];
                if (h.State == 2) continue;
                h.Fn?.Invoke();
            }
        }

        public void Clear()
        {
            _timers.Clear();
            _prepare.Clear();
            _pending.Clear();
            Now = 0;
        }
    }
}
