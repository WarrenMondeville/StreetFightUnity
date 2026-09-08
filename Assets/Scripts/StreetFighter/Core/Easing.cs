namespace StreetFighter
{
    /// <summary>
    /// 复刻 config.js 里的 easing 函数集合（原版是 JS 函数，这里按名字等价实现）。
    /// 签名与 JS 一致： f(t, b, c, d) = c * ease(t/d) + b
    /// </summary>
    public static class Easing
    {
        public static float Eval(string name, float t, float b, float c, float d)
        {
            switch (name)
            {
                case "easeIn":
                    { float x = t / d; return c * x * x + b; }
                case "strongEaseIn":
                    { float x = t / d; return c * x * x * x * x * x + b; }
                case "strongEaseOut":
                    { float x = t / d - 1f; return c * (x * x * x * x * x + 1f) + b; }
                case "sineaseIn":
                    { float x = t / d; return c * x * x * x + b; }
                case "sineaseOut":
                    { float x = t / d - 1f; return c * (x * x * x + 1f) + b; }
                case "sineaseInOut":
                    {
                        float x = t / (d / 2f);
                        if (x < 1f) return c / 2f * x * x + b;
                        x -= 1f;
                        return -c / 2f * (x * (x - 2f) - 1f) + b;
                    }
                default: // linear
                    return c * t / d + b;
            }
        }
    }
}
