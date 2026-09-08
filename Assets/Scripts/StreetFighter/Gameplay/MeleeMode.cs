namespace StreetFighter.Gameplay
{
    /// <summary>近身判定体的跟随方式。</summary>
    public enum MeleeMode
    {
        /// <summary>独立位移（按 easing 运动）。</summary>
        Normal,

        /// <summary>贴身在角色身上（空中组合技）。</summary>
        Stick,
    }
}
