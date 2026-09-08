using UnityEngine;
using UnityEngine.UI;

namespace StreetFighter
{
    /// <summary>
    /// 复刻 game.js 的 Blood：血量 1500，受击后血量条在最多 500ms 内线性收缩。
    /// 左条从左侧向内收缩，右条从右侧向内收缩。
    /// </summary>
    public class BloodBar
    {
        private const float FullBlood = 1500f;

        private readonly Image _img;
        private readonly bool _left;
        private readonly float _fullWidth;
        private readonly float _x;

        private float _blood = FullBlood;
        private float _currWidth;

        private float _animFrom;
        private float _animDelta;
        private float _animStart;
        private float _animDur;
        private bool _animating;

        public readonly Evt Event = new Evt();

        public BloodBar(Image img, bool left, float x, float fullWidth)
        {
            _img = img;
            _left = left;
            _x = x;
            _fullWidth = fullWidth;
            _currWidth = fullWidth;
        }

        public void Reduce(float count)
        {
            _blood -= count;

            float w = -count / FullBlood * _fullWidth;
            float dur = Mathf.Min(500f, Mathf.Abs(count * 1.5f));

            if (_animating) _currWidth = _animFrom + _animDelta;

            _animFrom = _currWidth;
            _animDelta = w;
            _animStart = Time.time * 1000f;
            _animDur = dur;
            _animating = true;

            if (_blood < 0) Event.Fire("empty");
        }

        public void Reload() => Reduce(_blood - FullBlood);

        public void Render()
        {
            if (_animating)
            {
                float t = (Time.time * 1000f - _animStart) / _animDur;
                if (t >= 1f)
                {
                    _currWidth = _animFrom + _animDelta;
                    _animating = false;
                }
                else
                {
                    _currWidth = Easing.Eval("linear", Time.time * 1000f - _animStart, _animFrom, _animDelta, _animDur);
                }
            }

            float w = Mathf.Clamp(_currWidth, 0f, _fullWidth);
            var rt = _img.rectTransform;
            rt.sizeDelta = new Vector2(w, rt.sizeDelta.y);
            rt.anchoredPosition = new Vector2(_left ? _x + _fullWidth - w : _x, rt.anchoredPosition.y);
        }
    }
}
