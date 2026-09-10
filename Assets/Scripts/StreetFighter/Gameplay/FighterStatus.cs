using StreetFighter.Core;
using UnityEngine;

namespace StreetFighter.Gameplay
{
    /// <summary>
    /// 维护攻击姿态 / 攻击等级 / 无敌 / 敌我距离分段，
    /// 是 AI、命中判定、防御、受击行为的共同依据。
    /// </summary>
    public sealed class FighterStatus
    {
        private const float NearDistance = 180f;
        private const float MiddleDistance = 350f;
        private const float FarDistance = 650f;

        /// <summary>config 里 attack_type = 2 表示「攻击判定中」。</summary>
        private const int AttackTypeIndex = 2;

        /// <summary>状态名里出现这个子串表示空中动作。</summary>
        private const string JumpToken = "jump";

        /// <summary>状态名里出现这个子串表示蹲姿动作。</summary>
        private const string CrouchToken = "crouch";

        /// <summary>状态名里出现这个子串表示轻攻击。</summary>
        private const string LightToken = "light";

        private static readonly AttackState[] AttackStates =
        {
            AttackState.Wait,
            AttackState.Defense,
            AttackState.Attack,
            AttackState.Beat,
            AttackState.FallDown,
        };

        private readonly Spirit _owner;
        private readonly GameClock _clock;

        private bool _isCustomInvincible;
        private bool _isPowerInvincible;

        public FighterStatus(Spirit owner, GameClock clock)
        {
            _owner = owner;
            _clock = clock;
            RefreshPoseFlags();
        }

        /// <summary>当前攻防姿态。</summary>
        public AttackState Attack { get; private set; } = AttackState.Wait;

        /// <summary>攻击等级，用于互拼判定。</summary>
        public int AttackPower { get; private set; }

        /// <summary>当前攻击是否为轻攻击。</summary>
        public bool IsAttackLight { get; private set; }

        /// <summary>当前状态是否属于跳跃类（缓存值，随 <c>Spirit.StateName</c> 变化刷新）。</summary>
        public bool IsJumping { get; private set; }

        /// <summary>当前状态是否属于蹲姿类（缓存值，随 <c>Spirit.StateName</c> 变化刷新）。</summary>
        public bool IsCrouching { get; private set; }

        /// <summary>攻击是否带有无敌（来自 attack_power 第二项）。</summary>
        public bool IsInvincible => _isPowerInvincible || _isCustomInvincible;

        /// <summary>敌我距离分段。</summary>
        public DistanceBand DistanceBand { get; private set; }

        /// <summary>敌我中心距离（像素）。</summary>
        public float Distance { get; private set; }

        /// <summary>重新计算与敌人的距离分段。</summary>
        public DistanceBand CheckEnemyDistance(Spirit enemy)
        {
            if (enemy == null)
            {
                return DistanceBand;
            }

            Distance = Mathf.Abs(_owner.Left + _owner.Width / 2f - (enemy.Left + enemy.Width / 2f));

            if (Distance < NearDistance)
            {
                DistanceBand = DistanceBand.Near;
            }
            else if (Distance < MiddleDistance)
            {
                DistanceBand = DistanceBand.Middle;
            }
            else if (Distance < FarDistance)
            {
                DistanceBand = DistanceBand.Far;
            }
            else
            {
                DistanceBand = DistanceBand.Furthest;
            }

            return DistanceBand;
        }

        /// <summary>按 config 的 attack_type 设置姿态。</summary>
        public AttackState SetAttackType(int configValue)
        {
            if (configValue == AttackTypeIndex)
            {
                IsAttackLight = _owner.StateName != null && _owner.StateName.Contains(LightToken);
            }

            if (configValue < 0 || configValue >= AttackStates.Length)
            {
                configValue = 0;
            }

            Attack = AttackStates[configValue];
            return Attack;
        }

        /// <summary>直接设置姿态。</summary>
        public void SetAttack(AttackState state) => Attack = state;

        /// <summary>设置攻击等级；第二项 &gt; 0 表示本次攻击带无敌。</summary>
        public void SetAttackPower(float[] power)
        {
            AttackPower = power != null && power.Length > 0 ? (int)power[0] : 0;
            _isPowerInvincible = power != null && power.Length > 1 && power[1] > 0;
        }

        /// <summary>短暂无敌（爬起来时使用）。</summary>
        public void SetInvincible(float durationMs)
        {
            _isCustomInvincible = true;
            _clock.Timeout(() => _isCustomInvincible = false, durationMs);
        }

        /// <summary>
        /// 重算跳跃 / 蹲姿标记。由 <c>Spirit.StateName</c> 的 setter 调用，
        /// 因此每帧读取姿态不需要再做字符串匹配。
        /// </summary>
        public void RefreshPoseFlags()
        {
            string name = _owner.StateName;
            IsJumping = name != null && name.Contains(JumpToken);
            IsCrouching = name != null && name.Contains(CrouchToken);
        }

        public bool IsJump() => IsJumping;

        public bool IsStand() => _owner.StateName != null && !IsJumping && !IsCrouching;

        public bool IsCrouch() => IsCrouching;
    }
}
