using UnityEngine;

namespace StreetFighter
{
    /// <summary>共享的舞台横向卷轴位置（原版是一个可滚动的 div）。</summary>
    public class StageScroll
    {
        public float ScrollLeft = 250f;
        public const float Min = 0f;
        public const float Max = 500f;
        public const float ContentWidth = 1400f;

        public void Add(float d) => ScrollLeft = Mathf.Clamp(ScrollLeft + d, Min, Max);
    }

    /// <summary>
    /// 复刻 map.js 的 Stage：角色顶到屏幕边缘时推动背景滚动，并反推对手。
    /// </summary>
    public class Stage
    {
        public static readonly StageScroll Bg = new StageScroll();

        private readonly Spirit _self;
        private float _oldScroll;
        private float _dis;
        private float _scrolling;

        public Stage(Spirit self, GameClock clock)
        {
            _self = self;
        }

        public void Begin() => _oldScroll = Bg.ScrollLeft;

        public void End() => _scrolling = 0f;

        public void Scroll(string dir)
        {
            _dis = dir == "left" ? -3f : 3f;
            _oldScroll = Bg.ScrollLeft;
            Bg.Add(_dis);
            if (_oldScroll != Bg.ScrollLeft) _scrolling = _dis;
            else End();
        }

        public void PushEnemy()
        {
            if (_oldScroll == Bg.ScrollLeft || _scrolling == 0f) return;
            _self.Enemy.Left -= _dis;
        }

        public bool IsScrolling() => _scrolling != 0f;

        public float ScrollValue() => _scrolling;
    }
}
