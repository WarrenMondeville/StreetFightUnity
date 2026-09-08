using System;
using System.Collections.Generic;
using UnityEngine;

namespace StreetFighter
{
    /// <summary>
    /// 原版 config.js 的 C# 只读视图。运行时从 Resources/config.json 载入，
    /// 保证数值与原版 100% 一致，避免手写常量出错。
    /// </summary>
    public static class Cfg
    {
        public const float MapWidth = 900f;
        public const float MapHeight = 490f;

        public static JVal Root { get; private set; }
        public static float Fps { get; private set; }
        public static float KeyFps { get; private set; }
        public static float Zoom { get; private set; }
        public static string BgBehind { get; private set; }
        public static string BgFront { get; private set; }
        public static string SpiritShadow { get; private set; }

        public static void Load(string text)
        {
            Root = Json.Parse(text);
            Fps = Root.Get("fps").F;
            KeyFps = Root.Get("key_fps").F;
            var map = Root.Get("map");
            Zoom = map.Get("spiritZoom").F;
            BgBehind = map.Get("bgBehind").S;
            BgFront = map.Get("bgFront").S;
            SpiritShadow = Root.Get("spiritShadow").S;
            if (Fps <= 0) Fps = 17f;
            if (Zoom <= 0) Zoom = 2.1f;
        }

        public static JVal Spirit(string key) => Root.Get("Spirit").Get(key);

        /// <summary>角色的 states 表。</summary>
        public static JVal States(string spiritKey) => Spirit(spiritKey).Get("states");

        /// <summary>Config.play[state]，可能不存在（原版同样会崩，这里做保护）。</summary>
        public static JVal Play(string state)
        {
            var p = Root.Get("play").Get(state);
            if (p.IsNull) Debug.LogWarning("[SF] Config.play 缺少状态: " + state);
            return p;
        }

        public static int PlayLock(string state)
        {
            var p = Play(state);
            return p.IsNull ? 0 : p.Get("lock").I;
        }

        // ---------- 常用字段读取 ----------

        public static string Bg(JVal st) => st.Get("bg").S;

        public static int FramesNum(JVal st)
        {
            int n = st.Get("framesNum").I;
            return n <= 0 ? 1 : n;
        }

        public static float EaseNum(JVal st, int i) => st.Get("easing").Get(i).F;

        public static string EaseFn(JVal st) => st.Get("easing").Get(3).S;

        public static bool EaseTopIsNull(JVal st)
        {
            var e = st.Get("easing");
            return e.Get(1).IsNull;
        }

        public static int[] Repeat(JVal st)
        {
            var r = st.Get("repeat");
            if (r.IsNull || r.Count == 0) return null;
            var arr = new int[r.Count];
            for (int i = 0; i < r.Count; i++) arr[i] = r.Get(i).I;
            return arr;
        }

        public static int AttackType(JVal st) => st.Get("attack_type").I;

        public static float[] NumArray(JVal st, string key)
        {
            var a = st.Get(key);
            if (a.IsNull) return null;
            var arr = new float[a.Count];
            for (int i = 0; i < a.Count; i++) arr[i] = a.Get(i).F;
            return arr;
        }

        public static string[] StrArray(JVal st, string key)
        {
            var a = st.Get(key);
            if (a.IsNull) return null;
            var arr = new string[a.Count];
            for (int i = 0; i < a.Count; i++) arr[i] = a.Get(i).S;
            return arr;
        }
    }
}
