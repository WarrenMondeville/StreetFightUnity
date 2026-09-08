using UnityEngine;

namespace StreetFighter
{
    /// <summary>
    /// 复刻 interface.js 的 StatusManage：维护攻击类型 / 攻击等级 / 无敌 / 敌我距离分段，
    /// 是 AI、命中判定、防御、受击行为的共同依据。
    /// </summary>
    public class Status
    {
        private static readonly string[] AttackTypes = { "wait", "defense", "attack", "beat", "fall_down" };

        private readonly Spirit _self;
        private readonly GameClock _clock;

        private float _distance;
        private string _distanceType;
        private string _attackType = "wait";
        private bool _attackLight;
        private int _attackPower;
        private bool _invincible;
        private bool _customInvincible;

        public Status(Spirit self, GameClock clock)
        {
            _self = self;
            _clock = clock;
        }

        public string CheckEnemyDistance(Spirit enemy)
        {
            if (enemy == null) return _distanceType;
            _distance = Mathf.Abs(_self.Left + _self.Width / 2f - (enemy.Left + enemy.Width / 2f));
            if (_distance < 180) _distanceType = "near";
            else if (_distance < 350) _distanceType = "middle";
            else if (_distance < 650) _distanceType = "far";
            else _distanceType = "furthest";
            return _distanceType;
        }

        public string SetAttackType(int type)
        {
            if (type == 2) _attackLight = _self.StateName != null && _self.StateName.Contains("light");
            if (type < 0 || type >= AttackTypes.Length) type = 0;
            return _attackType = AttackTypes[type];
        }

        public void SetAttackPower(float[] power)
        {
            _attackPower = power != null && power.Length > 0 ? (int)power[0] : 0;
            _invincible = power != null && power.Length > 1 && power[1] > 0;
        }

        /// <summary>短暂无敌（爬起来时）。</summary>
        public void SetInvincible(float time)
        {
            _customInvincible = true;
            _clock.Timeout(() => _customInvincible = false, time);
        }

        public bool IsJump() => _self.StateName != null && _self.StateName.Contains("jump");

        public bool IsStand() =>
            _self.StateName != null && !_self.StateName.Contains("jump") && !_self.StateName.Contains("crouch");

        public bool IsCrouch() => _self.StateName != null && _self.StateName.Contains("crouch");

        public bool Invincible => _invincible || _customInvincible;
        public int AttackPower => _attackPower;
        public string AttackType => _attackType;
        public bool AttackLight => _attackLight;
        public string DistanceType => _distanceType;
        public float Distance => _distance;
    }
}
