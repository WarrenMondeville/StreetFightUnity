using StreetFighter.Core;
using UnityEngine;
using UnityEngine.UI;

namespace StreetFighter.Game
{
    /// <summary>
    /// 血条：血量 1500，受击后血量条在最多 500ms 内线性收缩。
    /// 左条从左侧向内收缩，右条从右侧向内收缩。
    /// </summary>
    public sealed class BloodBar
    {
        private const float FullBlood = 1500f;
        private const float MaxShrinkDurationMs = 500f;
        private const float DurationPerDamage = 1.5f;

        private readonly GameClock _clock;
        private readonly Image _image;
        private readonly bool _anchoredLeft;
        private readonly float _originX;
        private readonly float _fullWidth;

        private float _blood = FullBlood;
        private float _currentWidth;

        private float _animationFrom;
        private float _animationDelta;
        private float _animationStart;
        private float _animationDuration;

        /// <summary>血条见底只需要通知一次，否则倒地后被继续命中会排入多次重开。</summary>
        private bool _isEmptyInvoked;
        private bool _isAnimating;

        /// <summary>血量见底时派发。</summary>
        public readonly EventBus Events = new EventBus();

        public BloodBar(GameClock clock, Image image, bool anchoredLeft, float originX, float fullWidth)
        {
            _clock = clock;
            _image = image;
            _anchoredLeft = anchoredLeft;
            _originX = originX;
            _fullWidth = fullWidth;
            _currentWidth = fullWidth;
        }

        /// <summary>扣血（传负数等于回血）。</summary>
        public void Reduce(float count)
        {
            _blood -= count;

            float delta = -count / FullBlood * _fullWidth;
            float duration = Mathf.Min(MaxShrinkDurationMs, Mathf.Abs(count * DurationPerDamage));

            if (_isAnimating)
            {
                _currentWidth = _animationFrom + _animationDelta;
            }

            _animationFrom = _currentWidth;
            _animationDelta = delta;
            _animationStart = (float)_clock.Now;
            _animationDuration = duration;
            _isAnimating = true;

            if (_blood < 0 && !_isEmptyInvoked)
            {
                _isEmptyInvoked = true;
                Events.Invoke(GameEvents.Empty);
            }
        }

        /// <summary>回满血，并允许再次触发见底事件。</summary>
        public void Reload()
        {
            _isEmptyInvoked = false;
            Reduce(_blood - FullBlood);
        }

        /// <summary>每渲染帧推进收缩动画并写回 UI。</summary>
        public void Render()
        {
            if (_isAnimating)
            {
                // 与全局逻辑时钟同源，暂停时血条收缩也会一并冻结
                float elapsed = (float)_clock.Now - _animationStart;
                if (elapsed / _animationDuration >= 1f)
                {
                    _currentWidth = _animationFrom + _animationDelta;
                    _isAnimating = false;
                }
                else
                {
                    _currentWidth = Easing.Evaluate(EasingName.Linear, elapsed, _animationFrom, _animationDelta,
                        _animationDuration);
                }
            }

            float width = Mathf.Clamp(_currentWidth, 0f, _fullWidth);
            var rectTransform = _image.rectTransform;
            rectTransform.sizeDelta = new Vector2(width, rectTransform.sizeDelta.y);
            rectTransform.anchoredPosition = new Vector2(
                _anchoredLeft ? _originX + _fullWidth - width : _originX,
                rectTransform.anchoredPosition.y);
        }
    }
}
