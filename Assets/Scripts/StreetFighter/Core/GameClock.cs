using System;
using System.Collections.Generic;

namespace StreetFighter.Core
{
    /// <summary>
    /// 固定步长的游戏时钟：所有子系统共用一个 17ms 的逻辑帧，
    /// 保证手感与帧数相关的判定在任何机器上都一致。
    /// 回调按「后注册的先执行」顺序触发；另外提供基于游戏内时间的延时调用，
    /// 因此暂停时 <see cref="Timeout"/> 也会一并冻结。
    /// </summary>
    public sealed class GameClock
    {
        /// <summary>一个逻辑帧的时长（毫秒）。</summary>
        public const int TickMilliseconds = 17;

        /// <summary>定时器状态。</summary>
        public enum TimerState
        {
            /// <summary>刚创建，尚未进入队列。</summary>
            Idle = 0,

            /// <summary>在队列中，每帧执行。</summary>
            Active = 1,

            /// <summary>已暂停，保留在队列中但不执行。</summary>
            Stopped = 2,
        }

        /// <summary>定时器句柄，由 <see cref="Add"/> 创建后可反复 Start / Stop。</summary>
        public sealed class TimerHandle
        {
            /// <summary>每帧回调。</summary>
            public Action Callback;

            /// <summary>当前状态。</summary>
            public TimerState State;
        }

        private struct ScheduledCallback
        {
            public double DueTime;
            public Action Callback;
        }

        private readonly List<TimerHandle> _timers = new List<TimerHandle>();
        private readonly List<TimerHandle> _pending = new List<TimerHandle>();
        private readonly List<ScheduledCallback> _scheduled = new List<ScheduledCallback>();

        /// <summary>游戏内累计时间（毫秒）。</summary>
        public double Now { get; private set; }

        /// <summary>时钟是否在推进（目前仅作标记，暂停由 <c>GameManager</c> 控制）。</summary>
        public bool IsRunning { get; set; }

        /// <summary>创建一个默认处于 <see cref="TimerState.Idle"/> 的定时器。</summary>
        public TimerHandle Add(Action callback) => new TimerHandle { Callback = callback, State = TimerState.Idle };

        /// <summary>把定时器放入队列（已在队列中的会恢复执行）。</summary>
        public void Start(TimerHandle handle)
        {
            if (handle == null)
            {
                return;
            }

            if (handle.State == TimerState.Idle)
            {
                handle.State = TimerState.Active;
                _pending.Insert(0, handle);
            }
            else if (handle.State == TimerState.Stopped)
            {
                handle.State = TimerState.Active;
            }
        }

        /// <summary>暂停定时器（保留在队列中）。</summary>
        public void Stop(TimerHandle handle)
        {
            if (handle != null)
            {
                handle.State = TimerState.Stopped;
            }
        }

        /// <summary>直接插到队首，也即在每帧最后执行（原版用于清屏）。</summary>
        public void Push(TimerHandle handle)
        {
            if (handle == null)
            {
                return;
            }

            if (!_timers.Contains(handle))
            {
                _timers.Insert(0, handle);
            }

            handle.State = TimerState.Active;
        }

        /// <summary>延时调用，使用游戏内时间。</summary>
        public void Timeout(Action callback, double delayMs)
        {
            _scheduled.Add(new ScheduledCallback { DueTime = Now + delayMs, Callback = callback });
        }

        /// <summary>推进一个逻辑帧。</summary>
        public void Tick()
        {
            Now += TickMilliseconds;

            for (int i = _scheduled.Count - 1; i >= 0; i--)
            {
                if (_scheduled[i].DueTime <= Now)
                {
                    var scheduled = _scheduled[i];
                    _scheduled.RemoveAt(i);
                    scheduled.Callback?.Invoke();
                }
            }

            for (int i = _pending.Count - 1; i >= 0; i--)
            {
                _timers.Insert(0, _pending[i]);
            }

            if (_pending.Count > 0)
            {
                _pending.Clear();
            }

            for (int i = _timers.Count - 1; i >= 0; i--)
            {
                var handle = _timers[i];
                if (handle.State == TimerState.Stopped)
                {
                    continue;
                }

                handle.Callback?.Invoke();
            }
        }

        /// <summary>清空所有定时器并归零时间。</summary>
        public void Clear()
        {
            _timers.Clear();
            _pending.Clear();
            _scheduled.Clear();
            Now = 0;
        }
    }
}
