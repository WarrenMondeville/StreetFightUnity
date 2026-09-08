using UnityEngine;

namespace StreetFighter
{
    /// <summary>
    /// 复刻 main.js 的 Fighter：跟随角色 / 独立移动的近身攻击判定体。
    /// attack_config 的含义（索引）：
    /// 0 偏移x, 1 偏移y, 2 位移x, 3 位移y, 4 时长, 5 缓动名, 6 特效, 7 受击状态, 8 伤害
    /// </summary>
    public class Melee : IBody, IAniOwner
    {
        private readonly GameClock _clock;
        private readonly Spirit _master;
        private readonly GameClock.Handle _timer;
        private readonly Ani _ani;
        private readonly Collider _col;
        private readonly AttackEffect _fx;

        private float[] _num;
        private string[] _str;
        private float[] _effectPos;
        private string[] _sound;

        public float Left { get; set; }
        public float Top { get; set; }
        public float Width { get; set; } = 50f;
        public float Height { get; set; } = 50f;
        public object Master => _master;
        public float FTop => 0f;

        public string AnimateType = "normal";
        public readonly Sfx Audio;

        public Melee(GameClock clock, Spirit master)
        {
            _clock = clock;
            _master = master;
            _ani = new Ani(clock, this);
            _col = new Collider(this);
            _fx = new AttackEffect(clock, null);
            Audio = new Sfx();

            _ani.Event.Listen("framesDone", Stop);
            _col.Event.Listen("affirm", OnAffirm);

            _master.Enemy.BloodBar.Event.Listen("empty", OnEnemyEmpty);

            _timer = clock.Add(Tick);
        }

        public float CrossBorder(float left)
        {
            float maxX = Cfg.MapWidth;
            if (left < 15f || left > maxX - Width - 20f) Stop();
            return left;
        }

        private void Tick()
        {
            if (_num == null) return;

            if (AnimateType == "stick")
            {
                if (_master.Direction == 1) Left = _master.Left + _num[0];
                else Left = _master.Left + _master.Width - _num[0] + Width;
                Top = _master.Top + _num[1];
            }
            else
            {
                _ani.Move();
            }

            _col.Check();
        }

        public void Start(float offX, float offY, float[] num, string[] str, float[] effectPos, string[] sound)
        {
            if (_master.Direction == 1) Left = _master.Left + offX;
            else Left = _master.Left + _master.Width * 2f - offX - Width * 2f;

            Top = _master.Top + offY;
            _effectPos = effectPos;
            _num = num;
            _str = str;
            _sound = sound;
            _ani.Start(num[0], num[1], num[2], str[3]);
            _clock.Start(_timer);
        }

        /// <summary>空中组合技：判定体贴在角色身上。</summary>
        public void StickStart(float[] num, string[] str, float[] effectPos, string[] sound)
        {
            _num = num;
            _str = str;
            _effectPos = effectPos;
            _sound = sound;
            Width = Height = num[2];
            AnimateType = "stick";
            _clock.Start(_timer);
            if (sound != null && sound.Length > 0) PlayAudio(sound[0], -1);
        }

        public void Stop()
        {
            AnimateType = "normal";
            _clock.Stop(_timer);
            _master.St.SetAttackPower(new[] { 0f, 0f });
            PlayAudio(null, 0);
        }

        public void PlayAudio(string src, int type)
        {
            if (!string.IsNullOrEmpty(src))
            {
                Audio.Play(src);
                return;
            }
            if (_sound == null) return;

            if (type == 1)
            {
                _master.Enemy.Attack.Audio.Play(_sound.Length > 1 ? _sound[1] : null);
                return;
            }
            if (type == 0)
            {
                if (_master.St.IsJump()) return;
                string eat = _master.Enemy.St.AttackType;
                if (eat != "beat" && eat != "fall_down" && _master.St.AttackType == "attack")
                    Audio.Play(_sound[0]);
            }
        }

        public void Render(float zoom) => _fx.Render(zoom);

        private void OnEnemyEmpty()
        {
            _clock.Timeout(() => GameManager.Instance?.Reload(), 3000);
            _master.Keys.Stop();
            if (_master.Enemy.St.IsJump()) _master.Enemy.Play("jump_dead");
            else _master.Enemy.Play("dead");
        }

        private void OnAffirm()
        {
            var other = _col.Other;
            if (!ReferenceEquals(other, _master.Enemy)) return;

            if (_master.St.IsStand() && _master.Enemy.St.IsCrouch() && !_master.StateName.Contains("near"))
            {
                Stop();
                return;
            }

            if (_master.Enemy.St.AttackType == "fall_down" || _master.Enemy.St.Invincible)
            {
                Stop();
                return;
            }

            if (_master.Enemy.St.AttackType == "defense")
            {
                EnemyDefense();
                return;
            }

            int power = _master.St.AttackPower;
            int enemyPower = _master.Enemy.St.AttackPower;

            if (power < enemyPower)
            {
                _master.Enemy.Attack.Stop();
                _master.Enemy.Attack.EnemyBeat();
                Stop();
                return;
            }

            if (power > 0 && power == enemyPower) _master.Enemy.Attack.EnemyBeat();

            EnemyBeat();
        }

        public void EnemyDefense()
        {
            if (_master.St.IsCrouch() && _master.Enemy.St.IsStand())
            {
                EnemyBeat();
                return;
            }

            float x = _master.Direction == 1
                ? _master.Enemy.Left + _effectPos[0]
                : _master.Enemy.Left + _master.Enemy.Width + _effectPos[0];
            _fx.Start("defense", x, _master.Enemy.Top + _effectPos[1], _master.Direction);

            bool light = _master.St.AttackLight;
            var spirit = (_master.Enemy.Border != null && !_master.St.IsJump()) ? _master : _master.Enemy;
            spirit.Ani.Start((light ? -20f : -70f) * spirit.Direction, 0f, 200f, "linear");

            var st = _master.States.Get(_master.StateName);
            float defenseBlood = st.Get("defenseBlood").F;
            if (defenseBlood > 0) _master.Enemy.BloodBar.Reduce(defenseBlood);

            _master.Enemy.Attack.Audio.Play("sound/defense.mp3");
        }

        public void EnemyBeat()
        {
            if ((_master.StateName == "jump_whirl_kick" || _master.StateName == "jump_light_whirl_kick")
                && _master.Enemy.St.IsCrouch())
                return;

            float x = _master.Direction == 1
                ? _master.Enemy.Left + _effectPos[0]
                : _master.Enemy.Left + _master.Enemy.Width + _effectPos[0];
            _fx.Start(_str[4], x, _master.Enemy.Top + _effectPos[1], _master.Direction);

            bool light = _master.St.AttackLight;

            bool whirl = _master.StateName == "jump_whirl_kick" || _master.StateName == "jump_light_whirl_kick"
                || _master.StateName == "jump_heavy_impact_boxing" || _master.StateName == "jump_light_impact_boxing";

            if (!_master.St.IsCrouch() && _master.Enemy.St.IsJump() && !whirl)
                _master.Enemy.Play("jump_fall_down");
            else
                _master.Enemy.Play(_str[5], true);

            if (_master.Enemy.Border != null && !_master.St.IsJump())
                _master.Ani.Start((light ? -70f : -150f) * _master.Direction, 0f, 300f, "linear");

            _master.Enemy.BloodBar.Reduce(_num[6]);
            _master.Enemy.WaveBoxing.ReadyFiring = false;

            PlayAudio(null, 1);
            Stop();
        }
    }
}
