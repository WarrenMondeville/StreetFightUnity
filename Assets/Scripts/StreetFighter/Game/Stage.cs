using StreetFighter.Gameplay;

namespace StreetFighter.Game
{
    /// <summary>
    /// 舞台：角色顶到屏幕边缘时推动背景滚动，并反推对手。
    /// 每帧的滚动量是固定的，能否滚动取决于卷轴是否已到端点。
    /// </summary>
    public sealed class Stage
    {
        private const float ScrollStep = 3f;

        /// <summary>全局共享的舞台卷轴。</summary>
        public static readonly StageScroll Background = new StageScroll();

        private readonly Spirit _owner;

        private float _oldScroll;
        private float _distance;
        private float _scrolling;

        public Stage(Spirit owner)
        {
            _owner = owner;
        }

        /// <summary>记录本帧开始时的卷轴位置。</summary>
        public void Begin() => _oldScroll = Background.ScrollLeft;

        public void End() => _scrolling = 0f;

        /// <summary>朝指定方向推动舞台。</summary>
        public void Scroll(Side side)
        {
            _distance = side == Side.Left ? -ScrollStep : ScrollStep;
            _oldScroll = Background.ScrollLeft;
            Background.Add(_distance);

            if (_oldScroll != Background.ScrollLeft)
            {
                _scrolling = _distance;
            }
            else
            {
                End();
            }
        }

        /// <summary>把对手按本帧的滚动量反向推开。</summary>
        public void PushEnemy()
        {
            if (_oldScroll == Background.ScrollLeft || _scrolling == 0f)
            {
                return;
            }

            _owner.Enemy.Left -= _distance;
        }

        public bool IsScrolling => _scrolling != 0f;

        public float ScrollValue => _scrolling;
    }
}
