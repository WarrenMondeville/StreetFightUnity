namespace StreetFighter.Gameplay
{
    /// <summary>
    /// 角色的攻防姿态，对应 config.json 里的 attack_type（0~4）。
    /// </summary>
    public enum AttackState
    {
        /// <summary>空闲。</summary>
        Wait = 0,

        /// <summary>防御中。</summary>
        Defense = 1,

        /// <summary>攻击判定中。</summary>
        Attack = 2,

        /// <summary>被击中硬直。</summary>
        Beat = 3,

        /// <summary>倒地。</summary>
        FallDown = 4,
    }
}
