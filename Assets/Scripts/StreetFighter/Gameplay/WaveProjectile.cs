using StreetFighter.Core;
using StreetFighter.View;
using UnityEngine;

namespace StreetFighter.Gameplay
{
    /// <summary>
    /// 波动拳：延迟发射的飞行道具，可与对方波动拳相消。
    /// light_wave / heavy_wave 的 attack_config：
    /// 0 尺寸偏移x，1 偏移y，2 判定体尺寸，3 未使用，4 消失特效，5 受击状态，6 伤害，7 防御伤害。
    /// </summary>
    public sealed class WaveProjectile : ICollidable, IMovable
    {
        private const float DefaultWidth = 56f;
        private const float DefaultHeight = 32f;
        private const float ColliderWidth = 48f;
        private const float ColliderHeight = 32f;

        private const float FireDelayMs = 150f;
        private const float VerticalOffset = 40f;
        private const float MirroredHorizontalOffset = 90f;
        private const float ForwardHorizontalOffset = 50f;
        private const float HitHeightTolerance = 130f;
        private const float MapPaddingLeft = 15f;

        private const int TextDisappearEffect = 4;
        private const int TextBeatState = 5;
        private const int ValueDamage = 6;
        private const int ValueDefenseDamage = 7;

        private readonly GameClock _clock;
        private readonly Spirit _master;
        private readonly FrameAnimator _frames;
        private readonly Mover _motion;
        private readonly BodyCollider _collider;
        private readonly AttackEffect _effects;

        private float[] _values;
        private string[] _texts;

        public WaveProjectile(GameClock clock, Spirit master)
        {
            _clock = clock;
            _master = master;
            _frames = new FrameAnimator(clock);
            _motion = new Mover(clock, this);
            _collider = new BodyCollider(this, ColliderWidth, ColliderHeight);
            _effects = new AttackEffect(clock);

            _frames.Events.AddListener(GameEvents.FrameStart, () =>
            {
                _motion.Move();
                if (IsFiring)
                {
                    _collider.Check();
                }
            });

            _collider.Hit += OnHit;

            _frames.Events.AddListener(GameEvents.FramesDone, () =>
            {
                _frames.Loop();
                _motion.Loop();
            });
        }

        #region ICollidable / IMovable

        public float Left { get; set; }

        public float Top { get; set; }

        public float Width { get; set; } = DefaultWidth;

        public float Height { get; set; } = DefaultHeight;

        public float FloorTop => 0f;

        public object Master => _master;

        public float CrossBorder(float left)
        {
            if (left < MapPaddingLeft || left > GameConfig.MapWidth - Width)
            {
                Stop();
            }

            return left;
        }

        #endregion

        public int Direction { get; private set; } = 1;

        /// <summary>飞行中。</summary>
        public bool IsFiring { get; private set; }

        /// <summary>已起手、等待正式发射。</summary>
        public bool IsReadyFiring { get; set; }

        /// <summary>特效控制器，供对手相消时使用。</summary>
        public AttackEffect Effects => _effects;

        /// <summary>发射波动拳。</summary>
        public void Start(int direction, JVal state)
        {
            string background = GameConfig.GetBackground(state);
            int frameCount = GameConfig.GetFrameCount(state);
            var easing = state.Get("easing");

            var attackConfig = state.Get("attack_config");
            _values = new float[attackConfig.Count];
            _texts = new string[attackConfig.Count];
            for (int i = 0; i < attackConfig.Count; i++)
            {
                _values[i] = attackConfig.Get(i).AsFloat;
                _texts[i] = attackConfig.Get(i).AsString;
            }

            IsReadyFiring = true;
            _clock.Timeout(() =>
            {
                if (!IsReadyFiring)
                {
                    return;
                }

                IsFiring = true;
                Direction = direction;
                Top = _master.Top + VerticalOffset;
                Left = direction == -1
                    ? _master.Left + Width - MirroredHorizontalOffset
                    : _master.Left + _master.Width + ForwardHorizontalOffset;

                _frames.Start(background, frameCount, easing.Get(2).AsInt, GameConfig.GetRepeatPattern(state),
                    state.Get("position").AsInt, direction);
                _motion.Start(easing.Get(0).AsFloat * direction, 0f, easing.Get(2).AsFloat * GameConfig.Fps * frameCount,
                    easing.Get(3).AsString);
            }, FireDelayMs);
        }

        public void Stop()
        {
            IsFiring = false;
            _frames.Stop();
        }

        public void Render(float zoom, int order)
        {
            if (!_frames.IsActive)
            {
                WaveView.Hide(_master.Key);
                _effects.Render(zoom);
                return;
            }

            var view = WaveView.Get(_master.Key, order);
            int sourceFrames = _frames.DrawSourceFrameCount <= 0 ? 1 : _frames.DrawSourceFrameCount;
            var size = SpriteLibrary.GetSize(_frames.DrawBackground);
            view.Show(_frames.DrawBackground, _frames.DrawFrame, sourceFrames, Left, Top,
                size.x / sourceFrames, size.y, Direction, zoom);
            _effects.Render(zoom);
        }

        private void OnHit(ICollidable other, Side side)
        {
            var enemyWave = _master.Enemy.Wave;

            if (ReferenceEquals(other, enemyWave) && IsFiring && enemyWave.IsFiring)
            {
                Stop();
                enemyWave.Stop();
                enemyWave.Effects.Start(_texts[TextDisappearEffect],
                    enemyWave.Direction == 1 ? Left - Width : Left + Width, Top, 1);
                _effects.Start(_texts[TextDisappearEffect], Left, Top, 1);
                return;
            }

            if (!ReferenceEquals(other, _master.Enemy))
            {
                return;
            }

            if (Mathf.Abs(Top - _master.Enemy.Top) > HitHeightTolerance)
            {
                return;
            }

            if (_master.Enemy.Status.IsInvincible)
            {
                return;
            }

            if (_master.Enemy.Status.Attack == AttackState.Defense)
            {
                _clock.Timeout(Stop, 0);
                EnemyDefense();
                return;
            }

            _clock.Timeout(Stop, 0);
            EnemyBeat();
        }

        /// <summary>对方防御成功：削少量血并把双方推开。</summary>
        public void EnemyDefense()
        {
            _master.Enemy.Attack.Audio.Play(SoundPaths.Defense);
            _master.Enemy.Wave.IsReadyFiring = false;
            _effects.Start(_texts[TextDisappearEffect], Left, Top, Direction);

            bool light = _master.Status.IsAttackLight;

            _master.Enemy.Play(_master.Enemy.Status.IsStand()
                ? StateNames.ForceStandUpDefense
                : StateNames.ForceStandCrouchDefense);

            _master.Enemy.Motion.Start((light ? -50f : -100f) * _master.Enemy.Direction, 0f, 300f, EasingNames.Linear);

            if (_master.Enemy.Border != Side.None
                && (_master.Status.DistanceBand == DistanceBand.Near || _master.Status.DistanceBand == DistanceBand.Middle))
            {
                _master.Motion.Start((light ? -50f : -100f) * _master.Direction, 0f, 300f, EasingNames.Linear);
            }

            _master.Enemy.BloodBar.Reduce(_values[ValueDefenseDamage]);
        }

        /// <summary>命中对方：击退、扣血。</summary>
        public void EnemyBeat()
        {
            _master.Enemy.Wave.IsReadyFiring = false;
            _effects.Start(_texts[TextDisappearEffect], Left, Top, Direction);

            bool light = _master.Status.IsAttackLight;

            _master.Enemy.Play(_master.Enemy.Status.IsJump()
                ? StateNames.HeavyAttackedFallDown
                : _texts[TextBeatState], true);

            if (_master.Enemy.Border != Side.None
                && (_master.Status.DistanceBand == DistanceBand.Near || _master.Status.DistanceBand == DistanceBand.Middle))
            {
                _master.Motion.Start((light ? -70f : -120f) * _master.Direction, 0f, 300f, EasingNames.Linear);
            }

            _master.Enemy.BloodBar.Reduce(_values[ValueDamage]);
            _master.Enemy.Attack.Audio.Play(SoundPaths.HitHeavyBoxing);
        }
    }
}
