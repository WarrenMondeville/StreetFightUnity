namespace StreetFighter.Core
{
    /// <summary>
    /// 测试模式开关：开启后由 <c>HitBoxOverlay</c> 在画面上叠加绘制攻击框与受击框。
    /// </summary>
    public static class DebugMode
    {
        /// <summary>是否开启判定框绘制。</summary>
        public static bool Enabled { get; private set; }

        /// <summary>切换开关并返回切换后的状态。</summary>
        public static bool Toggle() => Enabled = !Enabled;
    }
}
