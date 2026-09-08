using System;
using UnityEngine;

namespace StreetFighter
{
    /// <summary>空中组合技：在原动作上叠加播放另一段序列帧。</summary>
    public class ComboAttack
    {
        public string Bg;
        public int Curr;
        public int FramesNum;
        public int SourceFrames = 1;
        public int[] Repeat;
        public int AfterFrame = 2;

        private bool _locked;
        private Action _done;

        public bool Start(string bg, int framesNum, int[] repeat, int afterFrame)
        {
            if (_locked) return false;
            _locked = true;
            Bg = bg;
            Curr = 0;
            FramesNum = framesNum <= 0 ? 1 : framesNum;
            SourceFrames = FramesNum;
            Repeat = repeat;
            AfterFrame = afterFrame > 0 ? afterFrame : 2;
            if (Repeat != null)
            {
                int s = 0;
                for (int i = 0; i < Repeat.Length; i++) s += Repeat[i];
                FramesNum = s;
            }
            return true;
        }

        /// <summary>推进一帧；返回 null 表示当前没有组合技在播放。baseFrame 会被钳制（与原版一致）。</summary>
        public ComboAttack Get(ref int baseFrame, int baseFramesNum)
        {
            if (!_locked) return null;

            if (Curr >= FramesNum)
            {
                if (baseFrame < baseFramesNum - AfterFrame) baseFrame = baseFramesNum - AfterFrame;
                if (_done != null) _done();
            }
            if (Curr >= FramesNum) return null;

            Curr++;
            return this;
        }

        public void Stop()
        {
            Bg = null;
            Curr = 0;
            FramesNum = 0;
            Repeat = null;
            _locked = false;
        }

        public void Done(Action fn) => _done = fn;
    }

    /// <summary>
    /// 复刻 interface.js 的 SpiritFrames：逐帧推进 + 绘制当前帧。
    /// 绘制本身交给 View 层（每帧读取 DrawBg / DrawFrame）。
    /// </summary>
    public class SpiritFrames
    {
        private readonly GameClock _clock;
        private readonly GameClock.Handle _timer;

        public readonly Evt Event = new Evt();
        public readonly ComboAttack Combo = new ComboAttack();

        public string Bg;
        public int FramesNum = 1;
        public int Direction = 1;
        public int Position;

        private int _fFramesNum = 1;
        private int _multiple = 1;
        private int[] _repeat;
        private int _curr;
        private int _count;

        public string DrawBg;
        public int DrawFrame;
        public int DrawSource = 1;

        public SpiritFrames(GameClock clock)
        {
            _clock = clock;
            _timer = clock.Add(Tick);
        }

        public bool Active => _timer.State == 1;

        public void Start(string bg, int framesNum, int multiple, int[] repeat, int position, int direction)
        {
            _curr = 0;
            _count = 0;
            Bg = bg;
            FramesNum = framesNum <= 0 ? 1 : framesNum;
            _fFramesNum = FramesNum;
            if (multiple > 0) _multiple = multiple;
            _repeat = repeat;
            Position = position;
            Direction = direction;
            if (_repeat != null)
            {
                int s = 0;
                for (int i = 0; i < _repeat.Length; i++) s += _repeat[i];
                FramesNum = s;
            }
            Event.Fire("framesStart");
            _clock.Start(_timer);
        }

        public void Stop() => _clock.Stop(_timer);

        public void Loop()
        {
            if (!string.IsNullOrEmpty(Bg)) Start(Bg, _fFramesNum, 0, _repeat, Position, Direction);
        }

        private void Tick()
        {
            Event.Fire("frameStart");

            if (_count++ % _multiple != 0)
            {
                Draw();
                return;
            }

            if (_curr >= FramesNum)
            {
                Draw();
                _clock.Stop(_timer);
                Event.Fire("framesDone");
                return;
            }

            Draw();
            _curr++;
            Event.Fire("frameDone");
        }

        private void Draw()
        {
            int frame = Mathf.Min(_curr, FramesNum - 1);
            if (_repeat != null) frame = Really(frame, _repeat);

            var cb = Combo.Get(ref frame, _fFramesNum);
            if (cb != null)
            {
                DrawBg = cb.Bg;
                DrawSource = cb.SourceFrames;
                DrawFrame = Really(cb.Curr, cb.Repeat);
            }
            else
            {
                DrawBg = Bg;
                DrawSource = _fFramesNum;
                DrawFrame = frame;
            }
        }

        public static int Really(int frame, int[] repeat)
        {
            if (repeat == null) return frame;
            int k = 0;
            for (int i = 0; i < repeat.Length; i++)
            {
                k += repeat[i];
                if (k >= frame) return i;
            }
            return frame;
        }
    }
}
