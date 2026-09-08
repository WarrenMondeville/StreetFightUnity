using StreetFighter.Core;
using UnityEngine;

namespace StreetFighter.Gameplay
{
    /// <summary>
    /// 复刻 interface.js 的 Animate：基于时间的位移插值（横向 / 纵向），
    /// 并提供 lock / push / stagePush 等原版特有的推挤能力。
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
        private string _ease = EasingNames.Linear;
        private string _leftEase = EasingNames.Linear;
        private string _lastEase = EasingNames.Linear;
        private int _direction = 1;
        private Side _lockedSide = Side.None;
        private float[] _lastArgs;

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
        /// <param name="easeName">缓动函数名。</param>
        public void Start(float offsetX, float offsetY, float duration, string easeName)
        {
            _lastArgs = new[] { offsetX, offsetY, duration };
            _lastEase = easeName;
            _fromLeft = _owner.Left;
            _fromTop = _owner.Top;
            _offsetLeft = offsetX * _direction;
            _offsetTop = offsetY;
            _duration = duration;
            _startTime = (float)_clock.Now;
            _stageCount = 0;
            _ease = easeName;
            _leftEase = easeName;

            // 原版：挥空/受击的纵向缓动与横向不同，横向保持线性
            if (easeName == EasingNames.SineaseOut || easeName == EasingNames.SineaseIn)
            {
                _leftEase = EasingNames.Linear;
            }
        }

        /// <summary>用上一次的参数重播位移（循环动作使用）。</summary>
        public void Loop()
        {
            if (_lastArgs != null)
            {
                Start(_lastArgs[0], _lastArgs[1], _lastArgs[2], _lastEase);
            }
        }

        /// <summary>推进一帧位移。</summary>
        public void Move()
        {
            float elapsed = (float)_clock.Now - _startTime;

            if (_duration > 0 && elapsed >= _duration)
            {
                Events.Invoke(GameEvents.FramesDone);
            }

            Events.Invoke(GameEvents.FrameStart);

            if (_offsetLeft == 0 && _offsetTop == 0)
            {
                Events.Invoke(GameEvents.FrameDone);
                return;
            }

            float t = _duration <= 0 ? 1f : Mathf.Min(elapsed / _duration, 1f);

            float rawLeft = Easing.Evaluate(_leftEase, t, _fromLeft, _direction * _offsetLeft, 1f)
                            - _stageCount * _stageDistance;
            float newLeft = _owner.CrossBorder(rawLeft);

            if (_lockedSide == Side.Right && newLeft > _owner.Left && _offsetTop == 0)
            {
                return;
            }

            if (_lockedSide == Side.Left && newLeft < _owner.Left && _offsetTop == 0)
            {
                return;
            }

            _owner.Left = newLeft;
            _owner.Top = Easing.Evaluate(_ease, t, _fromTop, _offsetTop, 1f);

            Events.Invoke(GameEvents.FrameDone);

            if (_duration > 0 && elapsed >= _duration)
            {
                Events.Invoke(GameEvents.FramesDone);
            }
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
