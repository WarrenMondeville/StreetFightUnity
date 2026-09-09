namespace StreetFighter.Config
{
    /// <summary>
    /// 状态的 attack_config 种类。三类招式的判定字段含义不同，用这个枚举区分该读哪一份配置。
    /// </summary>
    public enum AttackConfigKind
    {
        /// <summary>没有判定配置（如波动拳起手姿势）。</summary>
        None = 0,

        /// <summary>近身攻击。</summary>
        Melee = 1,

        /// <summary>飞行道具。</summary>
        Wave = 2,

        /// <summary>空中组合技。</summary>
        Combo = 3,
    }
}
