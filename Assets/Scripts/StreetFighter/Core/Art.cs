using System.Collections.Generic;
using UnityEngine;

namespace StreetFighter
{
    /// <summary>
    /// 序列帧资源池。原版所有角色/特效图都是横向排列的 gif 条带，
    /// 这里已转成 png，运行时按 framesNum 横向切片成 Sprite。
    /// 切片使用 pivot(0,1)（左上角）+ PPU=1，方便直接沿用原版 left/top 像素坐标。
    /// </summary>
    public static class Art
    {
        private static readonly Dictionary<string, Texture2D> _tex = new Dictionary<string, Texture2D>();
        private static readonly Dictionary<string, Sprite[]> _sheets = new Dictionary<string, Sprite[]>();
        private static readonly Dictionary<string, Sprite> _whole = new Dictionary<string, Sprite>();
        private static readonly Dictionary<string, Sprite> _slices = new Dictionary<string, Sprite>();

        public static void Init()
        {
            var all = Resources.LoadAll<Texture2D>("Art");
            for (int i = 0; i < all.Length; i++) _tex[all[i].name] = all[i];
        }

        public static Texture2D Tex(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            Texture2D t;
            if (_tex.TryGetValue(name, out t)) return t;
            t = Resources.Load<Texture2D>("Art/" + name);
            if (t == null)
            {
                Debug.LogWarning("[SF] 找不到图片资源: " + name);
                return null;
            }
            _tex[name] = t;
            return t;
        }

        public static Vector2 Size(string name)
        {
            var t = Tex(name);
            return t == null ? Vector2.zero : new Vector2(t.width, t.height);
        }

        public static Sprite[] Sheet(string name, int frames)
        {
            if (frames <= 0) frames = 1;
            string key = name + "|" + frames;
            Sprite[] arr;
            if (_sheets.TryGetValue(key, out arr)) return arr;

            var tex = Tex(name);
            arr = new Sprite[frames];
            float w = tex.width / (float)frames;
            for (int i = 0; i < frames; i++)
            {
                var rect = new Rect(i * w, 0, w, tex.height);
                arr[i] = Sprite.Create(tex, rect, new Vector2(0f, 1f), 1f, 0, SpriteMeshType.FullRect);
            }
            _sheets[key] = arr;
            return arr;
        }

        public static Sprite Whole(string name)
        {
            Sprite s;
            if (_whole.TryGetValue(name, out s)) return s;
            var tex = Tex(name);
            if (tex == null) return null;
            s = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0f, 1f), 1f, 0, SpriteMeshType.FullRect);
            _whole[name] = s;
            return s;
        }

        /// <summary>按源矩形切片（特效图使用固定高度，与原版一致）。</summary>
        public static Sprite Slice(string name, int frame, int frames, float height)
        {
            string key = name + "|" + frames + "|" + frame + "|" + height;
            Sprite s;
            if (_slices.TryGetValue(key, out s)) return s;

            var tex = Tex(name);
            if (tex == null) return null;
            float w = tex.width / (float)frames;
            var rect = new Rect(frame * w, 0, w, height);
            s = Sprite.Create(tex, rect, new Vector2(0f, 1f), 1f, 0, SpriteMeshType.FullRect);
            _slices[key] = s;
            return s;
        }
    }
}
