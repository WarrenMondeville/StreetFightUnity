using StreetFighter.Core;
using StreetFighter.View;
using UnityEngine;

namespace StreetFighter.Gameplay
{
    /// <summary>
    /// 复刻 interface.js 的 AttackEffect：命中 / 防御 / 波动相消的特效序列帧。
    /// 每个特效图都是横向条带，帧数与高度由类型决定（与原版硬编码一致）。
    /// </summary>
    public sealed class AttackEffect
    {
        private const int FrameInterval = 5;
        private const string ObjectNamePrefix = "fx_";

        private readonly GameClock _clock;
        private readonly GameClock.TimerHandle _timer;

        private string _name;
        private int _frameCount = 3;
        private int _currentFrame;
        private int _tickCount;

        private SpriteView _view;

        public AttackEffect(GameClock clock)
        {
            _clock = clock;
            _timer = clock.Add(Tick);
        }

        public float Left { get; private set; }

        public float Top { get; private set; }

        public float Width { get; private set; }

        public float Height { get; private set; }

        public int Direction { get; private set; } = 1;

        /// <summary>本帧要绘制的帧号。</summary>
        public int DrawFrame { get; private set; }

        public bool IsPlaying => _timer.State == GameClock.TimerState.Active;

        public void Start(string type, float left, float top, int direction)
        {
            _frameCount = FrameCountOf(type);
            _currentFrame = 0;
            _tickCount = 0;
            _name = type;
            Left = left;
            Top = top;
            Direction = direction;

            var texture = SpriteLibrary.GetTexture(type);
            Width = texture != null ? texture.width / (float)_frameCount : 0f;
            Height = HeightOf(type);

            if (_view == null)
            {
                _view = new SpriteView(ObjectNamePrefix + type, 30);
            }

            _clock.Start(_timer);
        }

        /// <summary>每渲染帧同步到场景（原版是每帧直接 drawImage）。</summary>
        public void Render(float zoom)
        {
            if (_view == null)
            {
                return;
            }

            if (!IsPlaying)
            {
                _view.SetVisible(false);
                return;
            }

            _view.SetVisible(true);
            _view.Show(_name, DrawFrame, _frameCount, Left, Top, Width, Height, Direction, zoom, true);
        }

        private void Tick()
        {
            if (_tickCount++ % FrameInterval != 0)
            {
                _currentFrame = _currentFrame - 1;
            }

            DrawFrame = Mathf.Clamp(_currentFrame, 0, _frameCount - 1);
            _currentFrame++;

            if (_currentFrame >= _frameCount)
            {
                _clock.Stop(_timer);
            }
        }

        private static int FrameCountOf(string type)
        {
            switch (type)
            {
                case "heavy": return 4;
                case "defense": return 5;
                case "transverseWaveDisappear": return 5;
                default: return 3;
            }
        }

        private static float HeightOf(string type)
        {
            switch (type)
            {
                case "light": return 19f;
                case "heavy": return 31f;
                case "defense": return 32f;
                case "transverseWaveDisappear": return 28f;
                default: return 19f;
            }
        }
    }
}
