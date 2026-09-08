namespace StreetFighter.Gameplay
{
    /// <summary>
    /// 敌我距离分段，AI 与部分判定用它决定行为。
    /// 阈值与接口见 <see cref="FighterStatus.CheckEnemyDistance"/>。
    /// </summary>
    public enum DistanceBand
    {
        /// <summary>近距离（&lt; 180）。</summary>
        Near,

        /// <summary>中距离（&lt; 350）。</summary>
        Middle,

        /// <summary>远距离（&lt; 650）。</summary>
        Far,

        /// <summary>极远（&gt;= 650）。</summary>
        Furthest,
    }
}
