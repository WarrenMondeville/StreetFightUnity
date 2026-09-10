using StreetFighter.Core;
using UnityEngine;

namespace StreetFighter.Gameplay
{
    /// <summary>
    /// 基于时间的位移插值（横向 / 纵向），
    /// 并提供 lock / push / stagePush 等推挤能力。
    /// </summary>
    public sealed class Mover
    {
        private readonly GameClock _clock;
        private readonly IMovable _owner;

        private float _fromLeft;
        private float _fromTop;
        private float _offsetLeft;
        private float _offsetTop;
        private float _startTime;
        private float _duration;
        private float _stageDistance;
        private int _stageCount;
        private EasingName _ease = EasingName.Linear;
        private EasingName _leftEase = EasingName.Linear;
        private EasingName _lastEase = EasingName.Linear;
        private int _direction = 1;
        private Side _lockedSide = Side.None;

        /// <summary>上一次位移的参数，供 <see cref="Loop"/> 重播（用字段而非数组，避免每次起播都分配）。</summary>
        private bool _hasLastArgs;
        private float _lastOffsetX;
        private float _lastOffsetY;
        private float _lastDuration;

        /// <summary>位移过程中派发的事件（frameStart / frameDone / framesDone）。</summary>
        public readonly EventBus Events = new EventBus();

        public Mover(GameClock clock, IMovable owner)
        {
            _clock = clock;
            _owner = owner;
        }

        /// <summary>瞬移到指定位置。</summary>
        public void MoveTo(float left, float top)
        {
            _owner.Left = left;
            _owner.Top = top;
        }

        /// <summary>开始一段位移。</summary>
        /// <param name="offsetX">横向位移量（会乘以朝向）。</param>
        /// <param name="offsetY">纵向位移量。</param>
        /// <param name="duration">时长（毫秒）。</param>
        /// <param name="ease">缓动类型。</param>
        public void Start(float offsetX, float offsetY, float duration, EasingName ease)
        {
            _hasLastArgs = true;
            _lastOffsetX = offsetX;
            _lastOffsetY = offsetY;
            _lastDuration = duration;
            _lastEase = ease;
            _fromLeft = _owner.Left;
            _fromTop = _owner.Top;
            _offsetLeft = offsetX * _direction;
            _offsetTop = offsetY;
            _duration = duration;
            _startTime = (float)_clock.Now;
            _stageCount = 0;
            _ease = ease;
            _leftEase = ease;

            // 原版：挥空/受击的纵向缓动与横向不同，横向保持线性
            if (ease == EasingName.SineaseOut || ease == EasingName.SineaseIn)
            {
                _leftEase = EasingName.Linear;
            }
        }

        /// <summary>用上一次的参数重播位移（循环动作使用）。</summary>
        public void Loop()
        {
            if (_hasLastArgs)
            {
                Start(_lastOffsetX, _lastOffsetY, _lastDuration, _lastEase);
            }
        }

        /// <summary>推进一帧位移。</summary>
        public void Move()
        {
            float elapsed = (float)_clock.Now - _startTime;
            bool finished = _duration > 0 && elapsed >= _duration;

            Events.Invoke(GameEvents.FrameStart);

            if (_offsetLeft != 0 || _offsetTop != 0)
            {
                Apply(elapsed);
            }

            Events.Invoke(GameEvents.FrameDone);

            // 一段位移只以一次 framesDone 收尾（原先首尾各判一次，会重复触发）
            if (finished)
            {
                Events.Invoke(GameEvents.FramesDone);
            }
        }

        /// <summary>按缓动算出本帧位置并写回实体；被锁定方向时保持不动。</summary>
        private void Apply(float elapsed)
        {
            float t = _duration <= 0 ? 1f : Mathf.Min(elapsed / _duration, 1f);

            float rawLeft = Easing.Evaluate(_leftEase, t, _fromLeft, _direction * _offsetLeft, 1f)
                            - _stageCount * _stageDistance;
            float newLeft = _owner.CrossBorder(rawLeft);

            if (_offsetTop == 0)
            {
                if (_lockedSide == Side.Right && newLeft > _owner.Left)
                {
                    return;
                }

                if (_lockedSide == Side.Left && newLeft < _owner.Left)
                {
                    return;
                }
            }

            _owner.Left = newLeft;
            _owner.Top = Easing.Evaluate(_ease, t, _fromTop, _offsetTop, 1f);
        }

        /// <summary>被撞击 / 被推动时的横向位移。</summary>
        public void Push(float distance)
        {
            _owner.Left = _owner.CrossBorder(_owner.Left - distance * _direction);
        }

        /// <summary>锁定某个方向，禁止继续朝该方向移动。</summary>
        public void Lock(Side side) => _lockedSide = side;

        public void Unlock() => _lockedSide = Side.None;

        /// <summary>记录一次舞台推挤位移。</summary>
        public void StagePush(float distance)
        {
            _stageCount++;
            _stageDistance = distance;
        }

        public void StopStagePush() => _stageCount = 0;

        /// <summary>把纵向位置修正回地面基准。</summary>
        public void Correct() => _owner.Top = _owner.FloorTop;

        /// <summary>设置朝向；0 表示保持不变。</summary>
        public void Mirror(int direction)
        {
            if (direction != 0)
            {
                _direction = direction;
            }
        }
    }
}
