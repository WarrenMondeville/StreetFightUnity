using UnityEngine;

namespace StreetFighter
{
    /// <summary>
    /// 复刻 interface.js 的 AttackEffect：命中 / 防御 / 波动相消的特效序列帧。
    /// </summary>
    public class AttackEffect
    {
        private const int Multiple = 5;

        private readonly GameClock _clock;
        private readonly GameClock.Handle _timer;

        private string _name;
        private int _num = 3;
        private int _curr;
        private int _count;

        public float Left, Top, Width, Height;
        public int Dir = 1;
        public int DrawFrame;

        private SpriteView _view;

        public AttackEffect(GameClock clock, object master)
        {
            _clock = clock;
            _timer = clock.Add(Tick);
        }

        public bool Active => _timer.State == 1;
        public string Name => _name;
        public int Frames => _num;

        public void Start(string type, float left, float top, int dir)
        {
            _num = NumOf(type);
            _curr = 0;
            _count = 0;
            _name = type;
            Left = left;
            Top = top;
            Dir = dir;

            var tex = Art.Tex(type);
            Width = tex != null ? tex.width / (float)_num : 0f;
            Height = HeightOf(type);

            if (_view == null) _view = new SpriteView("fx_" + type, 30);
            _clock.Start(_timer);
        }

        private void Tick()
        {
            if (_count++ % Multiple != 0) _curr = _curr - 1;

            DrawFrame = Mathf.Clamp(_curr, 0, _num - 1);
            _curr++;

            if (_curr >= _num) _clock.Stop(_timer);
        }

        /// <summary>每渲染帧同步到场景（原版是每帧直接 drawImage）。</summary>
        public void Render(float zoom)
        {
            if (_view == null) return;
            if (!Active)
            {
                _view.SetVisible(false);
                return;
            }
            _view.SetVisible(true);
            _view.Show(_name, DrawFrame, _num, Left, Top, Width, Height, Dir, zoom, true);
        }

        private static int NumOf(string type)
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
