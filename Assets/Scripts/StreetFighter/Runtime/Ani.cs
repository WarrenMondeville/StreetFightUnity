using UnityEngine;

namespace StreetFighter
{
    /// <summary>Ani 的宿主：需要提供可读写的位置与边界钳制。</summary>
    public interface IAniOwner
    {
        float Left { get; set; }
        float Top { get; set; }
        float FTop { get; }
        float CrossBorder(float left);
    }

    /// <summary>
    /// 复刻 interface.js 的 Animate：基于时间的位移插值（横向 / 纵向），
    /// 并提供 lock / push / stagePush 等原版特有的推挤能力。
    /// </summary>
    public class Ani
    {
        private readonly GameClock _clock;
        private readonly IAniOwner _self;

        private float _fLeft, _fTop, _lLeft, _lTop, _startTime, _time;
        private float _stageDistance;
        private int _stageCount;
        private string _ease = "linear", _leftEase = "linear", _argEase = "linear";
        private int _dir = 1;
        private string _locked;
        private float[] _arg;

        public readonly Evt Event = new Evt();

        public Ani(GameClock clock, IAniOwner owner)
        {
            _clock = clock;
            _self = owner;
        }

        public void Moveto(float left, float top)
        {
            _self.Left = left;
            _self.Top = top;
        }

        public void Start(float left, float top, float t, string fn)
        {
            _arg = new[] { left, top, t };
            _argEase = fn;
            _fLeft = _self.Left;
            _fTop = _self.Top;
            _lLeft = left * _dir;
            _lTop = top;
            _time = t;
            _startTime = (float)_clock.Now;
            _stageCount = 0;
            _ease = fn;
            _leftEase = fn;
            if (fn == "sineaseOut" || fn == "sineaseIn") _leftEase = "linear";
        }

        public void Loop()
        {
            if (_arg != null) Start(_arg[0], _arg[1], _arg[2], _argEase);
        }

        public void Move()
        {
            float el = (float)_clock.Now - _startTime;

            if (_time > 0 && el >= _time) Event.Fire("framesDone");
            Event.Fire("frameStart");

            if (_lLeft == 0 && _lTop == 0)
            {
                Event.Fire("frameDone");
                return;
            }

            float t = _time <= 0 ? 1f : Mathf.Min(el / _time, 1f);

            float nl = Easing.Eval(_leftEase, t, _fLeft, _dir * _lLeft, 1f) - _stageCount * _stageDistance;
            float newLeft = _self.CrossBorder(nl);

            if (_locked == "right" && newLeft > _self.Left && _lTop == 0) return;
            if (_locked == "left" && newLeft < _self.Left && _lTop == 0) return;

            _self.Left = newLeft;
            _self.Top = Easing.Eval(_ease, t, _fTop, _lTop, 1f);

            Event.Fire("frameDone");

            if (_time > 0 && el >= _time) Event.Fire("framesDone");
        }

        /// <summary>被撞击 / 被推动时的横向位移。</summary>
        public void Push(float d)
        {
            _self.Left = _self.CrossBorder(_self.Left - d * _dir);
        }

        public void Lock(string dir) => _locked = dir;

        public void Unlock() => _locked = null;

        public void StagePush(float dis)
        {
            _stageCount++;
            _stageDistance = dis;
        }

        public void StopStagePush() => _stageCount = 0;

        public void Correct() => _self.Top = _self.FTop;

        public void Mirror(int dir)
        {
            if (dir != 0) _dir = dir;
        }
    }
}
