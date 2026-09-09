using StreetFighter.Config;
using StreetFighter.Core;
using StreetFighter.Game;
using UnityEngine;

namespace StreetFighter.Gameplay
{
    /// <summary>
    /// 近身攻击判定体。
    /// 普通攻击：判定体从角色身上生成后按配置独立位移；
    /// 空中组合技（<see cref="StickStart"/>）：判定体每帧贴在角色身上。
    /// </summary>
    public sealed class MeleeAttack : ICollidable, IMovable
    {
        private const float DefaultSize = 50f;
        private const float MapPaddingLeft = 15f;
        private const float MapPaddingRight = 20f;

        private const int StickSoundIndex = 0;

        /// <summary>一方血量见底后多久重开一局。</summary>
        private const float MatchEndDelayMs = 3000f;

        private readonly GameClock _clock;
        private readonly GameClock.TimerHandle _timer;
        private readonly Spirit _master;
        private readonly Mover _motion;
        private readonly BodyCollider _collider;
        private readonly AttackEffect _effects;

        private bool _hasAttack;
        private float[] _effectPosition;
        private string[] _sounds;
        private string _effect;
        private string _beatState;
        private float _damage;
        private float _stickOffsetX;
        private float _stickOffsetY;

        public MeleeAttack(GameClock clock, Spirit master)
        {
            _clock = clock;
            _master = master;
            _motion = new Mover(clock, this);
            _collider = new BodyCollider(this);
            _effects = new AttackEffect(clock);
            Audio = new AudioPlayer();

            _motion.Events.AddListener(GameEvents.FramesDone, Stop);
            _collider.Hit += OnHit;

            _master.Enemy.BloodBar.Events.AddListener(GameEvents.Empty, OnEnemyEmpty);

            _timer = clock.Add(Tick);
        }

        #region ICollidable / IMovable

        public float Left { get; set; }

        public float Top { get; set; }

        public float Width { get; set; } = DefaultSize;

        public float Height { get; set; } = DefaultSize;

        public float FloorTop => 0f;

        public object Master => _master;

        public float CrossBorder(float left)
        {
            if (left < MapPaddingLeft || left > GameConfig.MapWidth - Width - MapPaddingRight)
            {
                Stop();
            }

            return left;
        }

        #endregion

        /// <summary>当前跟随方式。</summary>
        public MeleeMode Mode { get; private set; } = MeleeMode.Normal;

        /// <summary>判定体是否正在生效（测试模式据此决定是否绘制攻击框）。</summary>
        public bool IsActive => _timer.State == GameClock.TimerState.Active;

        /// <summary>判定体，测试模式用它的实际尺寸绘制攻击框。</summary>
        public BodyCollider Collider => _collider;

        /// <summary>音效播放器，供外部（受击方）复用。</summary>
        public AudioPlayer Audio { get; }

        /// <summary>开始一次独立位移的攻击判定。</summary>
        public void Start(MeleeAttackConfig config, int direction, float[] effectPosition, string[] sounds)
        {
            Left = _master.Direction == 1
                ? _master.Left + config.OffsetX
                : _master.Left + _master.Width * 2f - config.OffsetX - Width * 2f;

            Top = _master.Top + config.OffsetY;
            Begin(config.Effect, config.BeatState, config.Damage, effectPosition, sounds);

            Mode = MeleeMode.Normal;
            _motion.Start(config.MoveX * direction, config.MoveY, config.Duration, config.Ease);
            _clock.Start(_timer);
        }

        /// <summary>空中组合技：判定体贴在角色身上。</summary>
        public void StickStart(ComboAttackConfig config, float[] effectPosition, string[] sounds)
        {
            Begin(config.Effect, config.BeatState, config.Damage, effectPosition, sounds);
            _stickOffsetX = config.OffsetX;
            _stickOffsetY = config.OffsetY;
            Width = Height = config.Size;
            Mode = MeleeMode.Stick;
            _clock.Start(_timer);

            if (sounds != null && sounds.Length > 0)
            {
                PlayAudio(sounds[StickSoundIndex], -1);
            }
        }

        public void Stop()
        {
            _hasAttack = false;
            Mode = MeleeMode.Normal;
            _clock.Stop(_timer);
            _master.Status.SetAttackPower(new[] { 0f, 0f });
            PlayAudio(null, 0);
        }

        /// <summary>两类攻击共用的命中表现参数。</summary>
        private void Begin(string effect, string beatState, float damage, float[] effectPosition, string[] sounds)
        {
            _hasAttack = true;
            _effect = effect;
            _beatState = beatState;
            _damage = damage;
            _effectPosition = effectPosition;
            _sounds = sounds;
        }

        /// <summary>
        /// 播放攻击音效。
        /// </summary>
        /// <param name="source">指定了就直接播放；否则按 type 选择击打音 / 受击音。</param>
        /// <param name="type">0 = 击打音，1 = 受击音，其余 = 静默。</param>
        public void PlayAudio(string source, int type)
        {
            if (!string.IsNullOrEmpty(source))
            {
                Audio.Play(source);
                return;
            }

            if (_sounds == null)
            {
                return;
            }

            if (type == 1)
            {
                _master.Enemy.Attack.Audio.Play(_sounds.Length > 1 ? _sounds[1] : null);
                return;
            }

            if (type == 0)
            {
                if (_master.Status.IsJump())
                {
                    return;
                }

                var enemyAttack = _master.Enemy.Status.Attack;
                if (enemyAttack != AttackState.Beat && enemyAttack != AttackState.FallDown && _master.Status.Attack == AttackState.Attack)
                {
                    Audio.Play(_sounds[0]);
                }
            }
        }

        public void Render(float zoom) => _effects.Render(zoom);

        private void Tick()
        {
            if (!_hasAttack)
            {
                return;
            }

            if (Mode == MeleeMode.Stick)
            {
                Left = _master.Direction == 1
                    ? _master.Left + _stickOffsetX
                    : _master.Left + _master.Width - _stickOffsetX + Width;
                Top = _master.Top + _stickOffsetY;
            }
            else
            {
                _motion.Move();
            }

            _collider.Check();
        }

        private void OnEnemyEmpty()
        {
            _clock.Timeout(() => GameManager.Instance?.Reload(), MatchEndDelayMs);
            _master.Keys.Stop();

            _master.Enemy.Play(_master.Enemy.Status.IsJump() ? StateNames.JumpDead : StateNames.Dead);
        }

        private void OnHit(ICollidable other, Side side)
        {
            if (!ReferenceEquals(other, _master.Enemy))
            {
                return;
            }

            if (_master.Status.IsStand()
                && _master.Enemy.Status.IsCrouch()
                && !_master.StateName.Contains(StateNames.NearPrefix))
            {
                Stop();
                return;
            }

            if (_master.Enemy.Status.Attack == AttackState.FallDown || _master.Enemy.Status.IsInvincible)
            {
                Stop();
                return;
            }

            if (_master.Enemy.Status.Attack == AttackState.Defense)
            {
                EnemyDefense();
                return;
            }

            int power = _master.Status.AttackPower;
            int enemyPower = _master.Enemy.Status.AttackPower;

            if (power < enemyPower)
            {
                _master.Enemy.Attack.Stop();
                _master.Enemy.Attack.EnemyBeat();
                Stop();
                return;
            }

            if (power > 0 && power == enemyPower)
            {
                _master.Enemy.Attack.EnemyBeat();
            }

            EnemyBeat();
        }

        /// <summary>对方防御成功：播放防御特效、双方推开、削一点血。</summary>
        public void EnemyDefense()
        {
            if (_master.Status.IsCrouch() && _master.Enemy.Status.IsStand())
            {
                EnemyBeat();
                return;
            }

            float x = _master.Direction == 1
                ? _master.Enemy.Left + _effectPosition[0]
                : _master.Enemy.Left + _master.Enemy.Width + _effectPosition[0];
            _effects.Start("defense", x, _master.Enemy.Top + _effectPosition[1], _master.Direction);

            bool light = _master.Status.IsAttackLight;
            var pushed = _master.Enemy.Border != Side.None && !_master.Status.IsJump() ? _master : _master.Enemy;
            pushed.Motion.Start((light ? -20f : -70f) * pushed.Direction, 0f, 200f, EasingNames.Linear);

            var state = _master.Config.GetState(_master.StateName);
            float defenseBlood = state != null ? state.DefenseBlood : 0f;
            if (defenseBlood > 0)
            {
                _master.Enemy.BloodBar.Reduce(defenseBlood);
            }

            _master.Enemy.Attack.Audio.Play(SoundPaths.Defense);
        }

        /// <summary>命中对方：播放受击特效、切换受击状态、扣血。</summary>
        public void EnemyBeat()
        {
            if ((_master.StateName == StateNames.JumpWhirlKick || _master.StateName == StateNames.JumpLightWhirlKick)
                && _master.Enemy.Status.IsCrouch())
            {
                return;
            }

            float x = _master.Direction == 1
                ? _master.Enemy.Left + _effectPosition[0]
                : _master.Enemy.Left + _master.Enemy.Width + _effectPosition[0];
            _effects.Start(_effect, x, _master.Enemy.Top + _effectPosition[1], _master.Direction);

            bool light = _master.Status.IsAttackLight;

            bool isWhirl = _master.StateName == StateNames.JumpWhirlKick
                           || _master.StateName == StateNames.JumpLightWhirlKick
                           || _master.StateName == StateNames.JumpHeavyImpactBoxing
                           || _master.StateName == StateNames.JumpLightImpactBoxing;

            if (!_master.Status.IsCrouch() && _master.Enemy.Status.IsJump() && !isWhirl)
            {
                _master.Enemy.Play(StateNames.JumpFallDown);
            }
            else
            {
                _master.Enemy.Play(_beatState, true);
            }

            if (_master.Enemy.Border != Side.None && !_master.Status.IsJump())
            {
                _master.Motion.Start((light ? -70f : -150f) * _master.Direction, 0f, 300f, EasingNames.Linear);
            }

            _master.Enemy.BloodBar.Reduce(_damage);
            _master.Enemy.Wave.IsReadyFiring = false;

            PlayAudio(null, 1);
            Stop();
        }
    }
}
