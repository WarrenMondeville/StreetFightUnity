using UnityEngine;

namespace StreetFighter.Core
{
    /// <summary>
    /// 游戏配置的 C# 只读视图。运行时从 <c>Resources/Config/</c> 下的分片 json 合并载入，
    /// 保证数值只来自配置，避免手写常量出错。
    /// </summary>
    public static class GameConfig
    {
        /// <summary>原版画布宽（像素）。</summary>
        public const float MapWidth = 900f;

        /// <summary>原版画布高（像素）。</summary>
        public const float MapHeight = 490f;

        private const float DefaultFps = 17f;
        private const float DefaultZoom = 2.1f;
        private const int DefaultEffectFrameCount = 3;
        private const float DefaultEffectHeight = 19f;

        /// <summary>配置根节点。</summary>
        public static JVal Root { get; private set; }

        /// <summary>逻辑帧时长（毫秒）。</summary>
        public static float Fps { get; private set; }

        /// <summary>输入采样间隔折算成的逻辑帧数。</summary>
        public static float KeyFps { get; private set; }

        /// <summary>角色缩放。</summary>
        public static float Zoom { get; private set; }

        /// <summary>远景背景图名。</summary>
        public static string BackgroundBehind { get; private set; }

        /// <summary>近景背景图名。</summary>
        public static string BackgroundFront { get; private set; }

        /// <summary>角色脚下阴影图名。</summary>
        public static string SpiritShadow { get; private set; }

        /// <summary>载入配置：读取 <c>Resources/Config</c> 下的全部分片并合并。</summary>
        public static void Load()
        {
            Root = ConfigLoader.Load();
            Fps = Root.Get("fps").AsFloat;
            KeyFps = Root.Get("key_fps").AsFloat;

            var map = Root.Get("map");
            Zoom = map.Get("spiritZoom").AsFloat;
            BackgroundBehind = map.Get("bgBehind").AsString;
            BackgroundFront = map.Get("bgFront").AsString;
            SpiritShadow = Root.Get("spiritShadow").AsString;

            if (Fps <= 0f)
            {
                Fps = DefaultFps;
            }

            if (Zoom <= 0f)
            {
                Zoom = DefaultZoom;
            }
        }

        /// <summary>Config.Spirit[key]。</summary>
        public static JVal GetSpirit(string key) => Root.Get("Spirit").Get(key);

        /// <summary>某个角色的 states 表。</summary>
        public static JVal GetStates(string spiritKey) => GetSpirit(spiritKey).Get("states");

        /// <summary>Config.play[state]，可能不存在（原版同样会崩，这里做保护）。</summary>
        public static JVal GetPlay(string state)
        {
            var play = Root.Get("play").Get(state);
            if (play.IsNull)
            {
                Debug.LogWarning($"[SF] Config.play 缺少状态: {state}");
            }

            return play;
        }

        /// <summary>动作优先级锁级别。</summary>
        public static int GetPlayLock(string state)
        {
            var play = GetPlay(state);
            return play.IsNull ? 0 : play.Get("lock").AsInt;
        }

        #region 常用字段读取

        /// <summary>状态对应的图集名。</summary>
        public static string GetBackground(JVal state) => state.Get("bg").AsString;

        /// <summary>状态帧数，至少为 1。</summary>
        public static int GetFrameCount(JVal state)
        {
            int frames = state.Get("framesNum").AsInt;
            return frames <= 0 ? 1 : frames;
        }

        /// <summary>easing 数组中的数值项。</summary>
        public static float GetEaseValue(JVal state, int index) => state.Get("easing").Get(index).AsFloat;

        /// <summary>easing 数组末位的缓动函数名。</summary>
        public static string GetEaseFunction(JVal state) => state.Get("easing").Get(3).AsString;

        /// <summary>easing 的 top 是否为 null（为 null 时需要按包围盒自动计算）。</summary>
        public static bool HasAutoTop(JVal state) => state.Get("easing").Get(1).IsNull;

        /// <summary>重复帧模式，没有则为 null。</summary>
        public static int[] GetRepeatPattern(JVal state)
        {
            var repeat = state.Get("repeat");
            if (repeat.IsNull || repeat.Count == 0)
            {
                return null;
            }

            var pattern = new int[repeat.Count];
            for (int i = 0; i < repeat.Count; i++)
            {
                pattern[i] = repeat.Get(i).AsInt;
            }

            return pattern;
        }

        /// <summary>attack_type，用于映射到 <c>AttackState</c>。</summary>
        public static int GetAttackType(JVal state) => state.Get("attack_type").AsInt;

        /// <summary>特效图的帧数（Config.hitEffect[type].framesNum）。</summary>
        public static int GetEffectFrameCount(string type)
        {
            int frames = Root.Get("hitEffect").Get(type).Get("framesNum").AsInt;
            return frames <= 0 ? DefaultEffectFrameCount : frames;
        }

        /// <summary>特效图的绘制高度（Config.hitEffect[type].height）。</summary>
        public static float GetEffectHeight(string type)
        {
            float height = Root.Get("hitEffect").Get(type).Get("height").AsFloat;
            return height <= 0f ? DefaultEffectHeight : height;
        }

        /// <summary>读取数值数组，字段缺失返回 null。</summary>
        public static float[] ReadFloatArray(JVal state, string key)
        {
            var array = state.Get(key);
            if (array.IsNull)
            {
                return null;
            }

            var values = new float[array.Count];
            for (int i = 0; i < array.Count; i++)
            {
                values[i] = array.Get(i).AsFloat;
            }

            return values;
        }

        /// <summary>读取字符串数组，字段缺失返回 null。</summary>
        public static string[] ReadStringArray(JVal state, string key)
        {
            var array = state.Get(key);
            if (array.IsNull)
            {
                return null;
            }

            var values = new string[array.Count];
            for (int i = 0; i < array.Count; i++)
            {
                values[i] = array.Get(i).AsString;
            }

            return values;
        }

        #endregion
    }
}
