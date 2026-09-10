namespace StreetFighter.Core
{
    /// <summary>
    /// 缓动函数类型，求值实现见 <see cref="Easing"/>。
    /// </summary>
    public enum EasingName
    {
        /// <summary>匀速。</summary>
        Linear,

        /// <summary>二次方加速。</summary>
        EaseIn,

        /// <summary>五次方加速。</summary>
        StrongEaseIn,

        /// <summary>五次方减速。</summary>
        StrongEaseOut,

        /// <summary>三次方加速。</summary>
        SineaseIn,

        /// <summary>三次方减速。</summary>
        SineaseOut,

        /// <summary>三次方先加速后减速。</summary>
        SineaseInOut,
    }
}
