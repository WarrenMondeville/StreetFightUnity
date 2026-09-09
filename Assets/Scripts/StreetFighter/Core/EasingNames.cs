namespace StreetFighter.Core
{
    /// <summary>
    /// 配置分片里用到的缓动函数名，C# 侧在 <see cref="Easing"/> 中按名字实现。
    /// </summary>
    public static class EasingNames
    {
        public const string Linear = "linear";
        public const string EaseIn = "easeIn";
        public const string StrongEaseIn = "strongEaseIn";
        public const string StrongEaseOut = "strongEaseOut";
        public const string SineaseIn = "sineaseIn";
        public const string SineaseOut = "sineaseOut";
        public const string SineaseInOut = "sineaseInOut";
    }
}
