using UnityEngine;

namespace StreetFighter
{
    /// <summary>
    /// 复刻 main.js 的 WaveBoxing（波动拳）：延迟发射的飞行道具，可与对方波动拳相消。
    /// light_wave / heavy_wave 的 attack_config：
    /// 0 尺寸偏移x, 1 偏移y, 2 判定体尺寸, 3 -, 4 消失特效, 5 受击状态, 6 伤害, 7 防御伤害
    /// </summary>
    public class Wave : IBody, IAniOwner
    {
        private readonly GameClock _clock;
        private readonly Spirit _master;
        private readonly SpiritFrames _frames;
        private readonly Ani _ani;
        private readonly Collider _col;
        private readonly AttackEffect _fx;

        private float[] _num;
        private string[] _str;

        public float Left { get; set; }
        public float Top { get; set; }
        public float Width { get; set; } = 56f;
        public float Height { get; set; } = 32f;
        public object Master => _master;
        public float FTop => 0f;

        public int Direction = 1;
        public bool Firing;
        public bool ReadyFiring;

        public AttackEffect Fx => _fx;

        public Wave(GameClock clock, Spirit master)
        {
            _clock = clock;
            _master = master;
            _frames = new SpiritFrames(clock);
            _ani = new Ani(clock, this);
            _col = new Collider(this, 48f, 32f);
            _fx = new AttackEffect(clock, this);

            _frames.Event.Listen("frameStart", () =>
            {
                _ani.Move();
                if (Firing) _col.Check();
            });

            _col.Event.Listen("affirm", OnAffirm);

            _frames.Event.Listen("framesDone", () =>
            {
                _frames.Loop();
                _ani.Loop();
            });
        }

        public float CrossBorder(float left)
        {
            if (left < 15f || left > Cfg.MapWidth - Width) Stop();
            return left;
        }

        public void Start(int dir, JVal state)
        {
            string bg = Cfg.Bg(state);
            int framesNum = Cfg.FramesNum(state);
            var easing = state.Get("easing");

            var ac = state.Get("attack_config");
            _num = new float[ac.Count];
            _str = new string[ac.Count];
            for (int i = 0; i < ac.Count; i++)
            {
                _num[i] = ac.Get(i).F;
                _str[i] = ac.Get(i).S;
            }

            ReadyFiring = true;
            _clock.Timeout(() =>
            {
                if (!ReadyFiring) return;

                Firing = true;
                Direction = dir;
                Top = _master.Top + 40f;
                if (dir == -1) Left = _master.Left + Width - 90f;
                else Left = _master.Left + _master.Width + 50f;

                _frames.Start(bg, framesNum, (int)easing.Get(2).F, Cfg.Repeat(state), (int)state.Get("position").F, dir);
                _ani.Start(easing.Get(0).F * dir, 0f, easing.Get(2).F * Cfg.Fps * framesNum, easing.Get(3).S);
            }, 150);
        }

        public void Stop()
        {
            Firing = false;
            _frames.Stop();
        }

        public void Render(float zoom, int order)
        {
            if (!_frames.Active)
            {
                WaveView.Hide(_master.Key);
                _fx.Render(zoom);
                return;
            }
            var v = WaveView.Get(_master.Key, order);
            int src = _frames.DrawSource <= 0 ? 1 : _frames.DrawSource;
            v.Show(_frames.DrawBg, _frames.DrawFrame, src, Left, Top,
                Art.Size(_frames.DrawBg).x / src, Art.Size(_frames.DrawBg).y, Direction, zoom);
            _fx.Render(zoom);
        }

        private void OnAffirm()
        {
            var other = _col.Other;
            var enemyWave = _master.Enemy.WaveBoxing;

            if (ReferenceEquals(other, enemyWave) && Firing && enemyWave.Firing)
            {
                Stop();
                enemyWave.Stop();
                enemyWave.Fx.Start(_str[4], enemyWave.Direction == 1 ? Left - Width : Left + Width, Top, 1);
                _fx.Start(_str[4], Left, Top, 1);
                return;
            }

            if (!ReferenceEquals(other, _master.Enemy)) return;

            if (Mathf.Abs(Top - _master.Enemy.Top) > 130f) return;
            if (_master.Enemy.St.Invincible) return;

            if (_master.Enemy.St.AttackType == "defense")
            {
                _clock.Timeout(Stop, 0);
                EnemyDefense();
                return;
            }

            _clock.Timeout(Stop, 0);
            EnemyBeat();
        }

        public void EnemyDefense()
        {
            _master.Enemy.Attack.Audio.Play("sound/defense.mp3");
            _master.Enemy.WaveBoxing.ReadyFiring = false;
            _fx.Start(_str[4], Left, Top, Direction);

            bool light = _master.St.AttackLight;

            if (_master.Enemy.St.IsStand()) _master.Enemy.Play("force_stand_up_defense");
            else _master.Enemy.Play("force_stand_crouch_defense");

            _master.Enemy.Ani.Start((light ? -50f : -100f) * _master.Enemy.Direction, 0f, 300f, "linear");

            string d = _master.St.DistanceType;
            if (_master.Enemy.Border != null && (d == "near" || d == "middle"))
                _master.Ani.Start((light ? -50f : -100f) * _master.Direction, 0f, 300f, "linear");

            _master.Enemy.BloodBar.Reduce(_num[7]);
        }

        public void EnemyBeat()
        {
            _master.Enemy.WaveBoxing.ReadyFiring = false;
            _fx.Start(_str[4], Left, Top, Direction);

            bool light = _master.St.AttackLight;

            if (_master.Enemy.St.IsJump()) _master.Enemy.Play("heavy_attacked_fall_down", true);
            else _master.Enemy.Play(_str[5], true);

            string d = _master.St.DistanceType;
            if (_master.Enemy.Border != null && (d == "near" || d == "middle"))
                _master.Ani.Start((light ? -70f : -120f) * _master.Direction, 0f, 300f, "linear");

            _master.Enemy.BloodBar.Reduce(_num[6]);
            _master.Enemy.Attack.Audio.Play("sound/hit_heavy_boxing.mp3");
        }
    }
}
