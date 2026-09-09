namespace StreetFighter.Core
{
    /// <summary>
    /// 缓动函数集合，按名字实现。
    /// 签名：<c>f(t, b, c, d) = c * ease(t / d) + b</c>。
    /// </summary>
    public static class Easing
    {
        /// <summary>按名字求值；未知名字按线性处理。</summary>
        /// <param name="name">缓动函数名。</param>
        /// <param name="t">已过时间。</param>
        /// <param name="b">起始值。</param>
        /// <param name="c">总变化量。</param>
        /// <param name="d">总时长。</param>
        public static float Evaluate(string name, float t, float b, float c, float d)
        {
            switch (name)
            {
                case "easeIn":
                {
                    float x = t / d;
                    return c * x * x + b;
                }

                case "strongEaseIn":
                {
                    float x = t / d;
                    return c * x * x * x * x * x + b;
                }

                case "strongEaseOut":
                {
                    float x = t / d - 1f;
                    return c * (x * x * x * x * x + 1f) + b;
                }

                case "sineaseIn":
                {
                    float x = t / d;
                    return c * x * x * x + b;
                }

                case "sineaseOut":
                {
                    float x = t / d - 1f;
                    return c * (x * x * x + 1f) + b;
                }

                case "sineaseInOut":
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
