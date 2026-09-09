namespace StreetFighter.Gameplay
{
    /// <summary>
    /// 角色状态名常量。值必须与配置分片中 <c>Spirit.*.states</c> 与 <c>play</c> 的键完全一致。
    /// </summary>
    public static class StateNames
    {
        #region 基础状态

        public const string Wait = "wait";
        public const string ForceWait = "force_wait";
        public const string Crouch = "crouch";
        public const string StandUp = "stand_up";
        public const string Forward = "forward";
        public const string Back = "back";
        public const string Dead = "dead";
        public const string JumpDead = "jump_dead";
        public const string AfterDead2 = "after_dead2";

        #endregion

        #region 跳跃 / 空中技

        public const string Jump = "jump";
        public const string JumpBack = "jump_back";
        public const string JumpForward = "jump_forward";
        public const string JumpForwardDown = "jump_forward_down";
        public const string JumpDown = "jumpDown";
        public const string JumpFallDown = "jump_fall_down";
        public const string JumpWhirlKick = "jump_whirl_kick";
        public const string JumpLightWhirlKick = "jump_light_whirl_kick";
        public const string LightJumpWhirlKick = "light_jump_whirl_kick";
        public const string JumpHeavyImpactBoxing = "jump_heavy_impact_boxing";
        public const string JumpLightImpactBoxing = "jump_light_impact_boxing";
        public const string SomesaultUp = "somesault_up";

        #endregion

        #region 受击 / 倒地

        public const string HeavyAttackedFallDown = "heavy_attacked_fall_down";
        public const string AfterHeavyAttackedFallDown = "after_heavy_attacked_fall_down";
        public const string CrouchKickAttackedFall = "crouch_kick_attacked_fall";

        #endregion

        #region 防御

        public const string StandUpDefense = "stand_up_defense";
        public const string StandCrouchDefense = "stand_crouch_defense";
        public const string ForceStandUpDefense = "force_stand_up_defense";
        public const string ForceStandCrouchDefense = "force_stand_crouch_defense";

        #endregion

        #region 位移

        public const string ForceBack = "force_back";
        public const string ForceForward = "force_forward";

        #endregion

        #region 攻击

        public const string HeavyKick = "heavy_kick";
        public const string CrouchHeavyKick = "crouch_heavy_kick";
        public const string CrouchLightKick = "crouch_light_kick";
        public const string CrouchHeavyBoxing = "crouch_heavy_boxing";
        public const string HeavyBoxing = "heavy_boxing";
        public const string LightBoxing = "light_boxing";
        public const string LightWaveBoxing = "light_wave_boxing";
        public const string HeavyWaveBoxing = "heavy_wave_boxing";
        public const string LightWave = "light_wave";
        public const string HeavyWave = "heavy_wave";

        #endregion

        #region 前缀与内部键

        /// <summary>蹲姿动作前缀，如 crouch + heavy_kick。</summary>
        public const string CrouchPrefix = "crouch_";

        /// <summary>近身动作前缀，如 near_ + heavy_kick。</summary>
        public const string NearPrefix = "near_";

        /// <summary>states 表中的空中组合技表。</summary>
        public const string Combo = "combo";

        /// <summary>states 表中的默认状态名。</summary>
        public const string Default = "default";

        #endregion
    }
}
