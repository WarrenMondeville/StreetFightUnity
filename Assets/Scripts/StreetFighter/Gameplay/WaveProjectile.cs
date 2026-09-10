using StreetFighter.Config;
using StreetFighter.Core;
using StreetFighter.View;
using UnityEngine;

namespace StreetFighter.Gameplay
{
    /// <summary>
    /// 波动拳：延迟发射的飞行道具，可与对方波动拳相消。
    /// 判定参数取自 light_wave / heavy_wave 状态的飞行道具配置。
    /// </summary>
    public sealed class WaveProjectile : ICollidable, IMovable
    {
        /// <summary>绘制尺寸（配置编辑器预览也用它）。</summary>
        public const float DefaultWidth = 56f;

        /// <summary>绘制高度。</summary>
        public const float DefaultHeight = 32f;

        /// <summary>判定框宽（小于绘制宽度，居中放置）。</summary>
        public const float ColliderWidth = 48f;

        /// <summary>判定框高。</summary>
        public const float ColliderHeight = 32f;

        private const float FireDelayMs = 150f;
        private const float VerticalOffset = 40f;
        private const float MirroredHorizontalOffset = 90f;
        private const float ForwardHorizontalOffset = 50f;
        private const float HitHeightTolerance = 130f;
        private const float MapPaddingLeft = 15f;

        private readonly GameClock _clock;
        private readonly Spirit _master;
        private readonly FrameAnimator _frames;
        private readonly Mover _motion;
        private readonly BodyCollider _collider;
        private readonly AttackEffect _effects;

        private WaveAttackConfig _wave;

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

        /// <summary>判定体，测试模式用它的实际尺寸绘制波动拳判定框。</summary>
        public BodyCollider Collider => _collider;

        /// <summary>特效控制器，供对手相消时使用。</summary>
        public AttackEffect Effects => _effects;

        /// <summary>发射波动拳。</summary>
        public void Start(int direction, StateConfig state)
        {
            var easing = state.Easing;
            int frameCount = state.FrameCount;
            _wave = state.WaveAttack;

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

                _frames.Start(state.Background, frameCount, (int)easing.Step, state.RepeatPattern,
                    state.Position, direction);
                _motion.Start(easing.Dx * direction, 0f, easing.Step * GameConfig.Fps * frameCount, easing.Ease);
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
                enemyWave.Effects.Start(_wave.DisappearEffect,
                    enemyWave.Direction == 1 ? Left - Width : Left + Width, Top, 1);
                _effects.Start(_wave.DisappearEffect, Left, Top, 1);
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
            _effects.Start(_wave.DisappearEffect, Left, Top, Direction);

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

            _master.Enemy.BloodBar.Reduce(_wave.DefenseDamage);
        }

        /// <summary>命中对方：击退、扣血。</summary>
        public void EnemyBeat()
        {
            _master.Enemy.Wave.IsReadyFiring = false;
            _effects.Start(_wave.DisappearEffect, Left, Top, Direction);

            bool light = _master.Status.IsAttackLight;

            _master.Enemy.Play(_master.Enemy.Status.IsJump()
                ? StateNames.HeavyAttackedFallDown
                : _wave.BeatState, true);

            if (_master.Enemy.Border != Side.None
                && (_master.Status.DistanceBand == DistanceBand.Near || _master.Status.DistanceBand == DistanceBand.Middle))
            {
                _master.Motion.Start((light ? -70f : -120f) * _master.Direction, 0f, 300f, EasingNames.Linear);
            }

            _master.Enemy.BloodBar.Reduce(_wave.Damage);
            _master.Enemy.Attack.Audio.Play(SoundPaths.HitHeavyBoxing);
        }
    }
}
