using System.Collections.Generic;
using UnityEngine;

namespace StreetFighter
{
    /// <summary>
    /// 一个带 SpriteRenderer 的 GameObject，负责把原版的 (left, top, width, height, direction, zoom)
    /// 画布坐标映射成 Unity 世界坐标。
    /// 原版画布 900x490，y 向下；这里把世界原点放在画布中心，1 世界单位 = 1 像素。
    /// </summary>
    public class SpriteView
    {
        protected readonly GameObject Go;
        protected readonly SpriteRenderer Sr;

        public SpriteView(string name, int order)
        {
            Go = new GameObject(name);
            Sr = Go.AddComponent<SpriteRenderer>();
            Sr.sortingOrder = order;
        }

        public void SetVisible(bool v)
        {
            if (Go.activeSelf != v) Go.SetActive(v);
        }

        /// <param name="srcW">源帧宽（像素），用于决定翻转后的包围盒。</param>
        public void Show(string bg, int frame, int sourceFrames, float left, float top,
            float srcW, float srcH, int dir, float zoom, bool fixedHeight = false)
        {
            if (string.IsNullOrEmpty(bg)) return;

            int frames = sourceFrames <= 0 ? 1 : sourceFrames;
            int idx = Mathf.Clamp(frame, 0, frames - 1);

            Sr.sprite = fixedHeight
                ? Art.Slice(bg, idx, frames, srcH)
                : Art.Sheet(bg, frames)[idx];

            // 翻转时把原点放到包围盒右侧，保证 [left, left + srcW*zoom] 不变
            float originX = dir == 1 ? left : left + srcW * zoom;
            Go.transform.position = new Vector3(originX - Cfg.MapWidth / 2f, Cfg.MapHeight / 2f - top, 0f);
            Go.transform.localScale = new Vector3(zoom * dir, zoom, 1f);
        }

        /// <summary>背景层：整张图拉伸到指定像素尺寸。</summary>
        public void ShowStretched(string bg, float left, float top, float w, float h)
        {
            var tex = Art.Tex(bg);
            if (tex == null) return;
            Sr.sprite = Art.Whole(bg);
            Go.transform.position = new Vector3(left - Cfg.MapWidth / 2f, Cfg.MapHeight / 2f - top, 0f);
            Go.transform.localScale = new Vector3(w / tex.width, h / tex.height, 1f);
        }
    }

    /// <summary>角色本体 + 脚下阴影。</summary>
    public class SpiritView
    {
        private readonly SpriteView _body;
        private readonly SpriteView _shadow;

        public SpiritView(string key, int order)
        {
            _body = new SpriteView(key, order);
            _shadow = new SpriteView(key + "_shadow", order - 1);
        }

        public void Render(Spirit s, float zoom, int order)
        {
            var f = s.Frames;
            float w = Art.Size(f.DrawBg).x / (f.DrawSource <= 0 ? 1 : f.DrawSource);
            float h = Art.Size(f.DrawBg).y;

            _body.Show(f.DrawBg, f.DrawFrame, f.DrawSource, s.Left, s.Top, w, h, f.Direction, zoom);

            var sh = Art.Size(Cfg.SpiritShadow);
            _shadow.Show(Cfg.SpiritShadow, 0, 1, s.ShadowLeft, Cfg.MapHeight - 73f,
                s.ShadowWidth, sh.y, s.Direction, zoom);
        }
    }

    /// <summary>波动拳飞行道具的视图（按角色键复用）。</summary>
    public static class WaveView
    {
        private static readonly Dictionary<string, WaveViewItem> Items = new Dictionary<string, WaveViewItem>();

        private class WaveViewItem
        {
            public readonly SpriteView View;
            public WaveViewItem(int order) => View = new SpriteView("wave_" + order, order);
        }

        public static SpriteView Get(string key, int order)
        {
            WaveViewItem item;
            if (!Items.TryGetValue(key, out item))
            {
                item = new WaveViewItem(order);
                Items[key] = item;
            }
            return item.View;
        }

        public static void Hide(string key)
        {
            WaveViewItem item;
            if (Items.TryGetValue(key, out item)) item.View.SetVisible(false);
        }
    }
}
