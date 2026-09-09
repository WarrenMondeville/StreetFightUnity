namespace StreetFighter.Core
{
    /// <summary>
    /// Input System 的动作名常量。招式动作直接沿用配置分片里的招式名，
    /// 这里只登记代码里固定创建的几个动作。
    /// </summary>
    public static class InputActionNames
    {
        /// <summary>移动（方向键 / 摇杆），值类型为 Vector2。</summary>
        public const string Move = "Move";

        /// <summary>暂停开关。</summary>
        public const string Pause = "Pause";

        /// <summary>切到人机模式。</summary>
        public const string VersusAi = "VersusAi";

        /// <summary>切到双人模式。</summary>
        public const string VersusPlayer = "VersusPlayer";

        /// <summary>测试模式开关（绘制攻击框 / 受击框）。</summary>
        public const string DebugHitBox = "DebugHitBox";
    }
}
