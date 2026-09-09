using StreetFighter.Core;
using UnityEngine;

namespace StreetFighter.Gameplay
{
    /// <summary>
    /// 逐帧推进序列帧并算出当前要绘制的图集与帧号。
    /// 绘制本身交给表现层（每帧读取 <see cref="DrawBackground"/> / <see cref="DrawFrame"/>）。
    /// </summary>
    public sealed class FrameAnimator
    {
        private readonly GameClock _clock;
        private readonly GameClock.TimerHandle _timer;

        /// <summary>序列帧事件（frameStart / frameDone / framesStart / framesDone）。</summary>
        public readonly EventBus Events = new EventBus();

        /// <summary>空中组合技，始终存在但只在播放时生效。</summary>
        public readonly ComboAttack Combo = new ComboAttack();

        /// <summary>当前图集名。</summary>
        public string Background { get; private set; }

        /// <summary>总帧数（含重复帧展开）。</summary>
        public int FrameCount { get; private set; } = 1;

        public int Direction { get; private set; } = 1;

        public int Position { get; private set; }

        /// <summary>本帧要绘制的图集。</summary>
        public string DrawBackground { get; private set; }

        /// <summary>本帧要绘制的帧号。</summary>
        public int DrawFrame { get; private set; }

        /// <summary>本帧图集的原始帧数（用于切片）。</summary>
        public int DrawSourceFrameCount { get; private set; } = 1;

        private int _sourceFrameCount = 1;
        private int _frameInterval = 1;
        private int[] _repeatPattern;
        private int _currentFrame;
        private int _tickCount;

        public FrameAnimator(GameClock clock)
        {
            _clock = clock;
            _timer = clock.Add(Tick);
        }

        /// <summary>定时器是否在运行。</summary>
        public bool IsActive => _timer.State == GameClock.TimerState.Active;

        /// <summary>开始播放一段序列帧。</summary>
        /// <param name="background">图集名。</param>
        /// <param name="frameCount">原始帧数。</param>
        /// <param name="frameInterval">每隔几帧推进一次（&lt;=0 表示沿用上次）。</param>
        /// <param name="repeatPattern">重复帧模式。</param>
        /// <param name="position">状态 position 标记。</param>
        /// <param name="direction">朝向。</param>
        public void Start(string background, int frameCount, int frameInterval, int[] repeatPattern, int position, int direction)
        {
            _currentFrame = 0;
            _tickCount = 0;
            Background = background;
            FrameCount = frameCount <= 0 ? 1 : frameCount;
            _sourceFrameCount = FrameCount;

            if (frameInterval > 0)
            {
                _frameInterval = frameInterval;
            }

            _repeatPattern = repeatPattern;
            Position = position;
            Direction = direction;

            if (_repeatPattern != null)
            {
                int total = 0;
                for (int i = 0; i < _repeatPattern.Length; i++)
                {
                    total += _repeatPattern[i];
                }

                FrameCount = total;
            }

            Events.Invoke(GameEvents.FramesStart);
            _clock.Start(_timer);
        }

        public void Stop() => _clock.Stop(_timer);

        /// <summary>循环播放当前图集。</summary>
        public void Loop()
        {
            if (!string.IsNullOrEmpty(Background))
            {
                Start(Background, _sourceFrameCount, 0, _repeatPattern, Position, Direction);
            }
        }

        /// <summary>把展开后的帧序号换算回图集里的真实帧。</summary>
        public static int ResolveFrame(int frame, int[] repeatPattern)
        {
            if (repeatPattern == null)
            {
                return frame;
            }

            int accumulated = 0;
            for (int i = 0; i < repeatPattern.Length; i++)
            {
                accumulated += repeatPattern[i];
                if (accumulated >= frame)
                {
                    return i;
                }
            }

            return frame;
        }

        private void Tick()
        {
            Events.Invoke(GameEvents.FrameStart);

            if (_tickCount++ % _frameInterval != 0)
            {
                Draw();
                return;
            }

            if (_currentFrame >= FrameCount)
            {
                Draw();
                _clock.Stop(_timer);
                Events.Invoke(GameEvents.FramesDone);
                return;
            }

            Draw();
            _currentFrame++;
            Events.Invoke(GameEvents.FrameDone);
        }

        private void Draw()
        {
            int frame = Mathf.Min(_currentFrame, FrameCount - 1);
            if (_repeatPattern != null)
            {
                frame = ResolveFrame(frame, _repeatPattern);
            }

            var combo = Combo.Next(ref frame, _sourceFrameCount);
            if (combo != null)
            {
                DrawBackground = combo.Background;
                DrawSourceFrameCount = combo.SourceFrameCount;
                DrawFrame = ResolveFrame(combo.CurrentFrame, combo.RepeatPattern);
                return;
            }

            DrawBackground = Background;
            DrawSourceFrameCount = _sourceFrameCount;
            DrawFrame = frame;
        }
    }
}
