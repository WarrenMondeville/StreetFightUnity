namespace StreetFighter.Core
{
    /// <summary>
    /// 缓动函数集合，按 <see cref="EasingName"/> 实现。
    /// 签名：<c>f(t, b, c, d) = c * ease(t / d) + b</c>。
    /// </summary>
    public static class Easing
    {
        /// <summary>按缓动类型求值。</summary>
        /// <param name="ease">缓动类型。</param>
        /// <param name="t">已过时间。</param>
        /// <param name="b">起始值。</param>
        /// <param name="c">总变化量。</param>
        /// <param name="d">总时长。</param>
        public static float Evaluate(EasingName ease, float t, float b, float c, float d)
        {
            switch (ease)
            {
                case EasingName.EaseIn:
                {
                    float x = t / d;
                    return c * x * x + b;
                }

                case EasingName.StrongEaseIn:
                {
                    float x = t / d;
                    return c * x * x * x * x * x + b;
                }

                case EasingName.StrongEaseOut:
                {
                    float x = t / d - 1f;
                    return c * (x * x * x * x * x + 1f) + b;
                }

                case EasingName.SineaseIn:
                {
                    float x = t / d;
                    return c * x * x * x + b;
                }

                case EasingName.SineaseOut:
                {
                    float x = t / d - 1f;
                    return c * (x * x * x + 1f) + b;
                }

                case EasingName.SineaseInOut:
                {
                    float x = t / (d / 2f);
                    if (x < 1f)
                    {
                        return c / 2f * x * x + b;
                    }

                    x -= 1f;
                    return -c / 2f * (x * (x - 2f) - 1f) + b;
                }

                default:
                    return c * t / d + b;
            }
        }
    }
}
