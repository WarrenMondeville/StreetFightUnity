using StreetFighter.Core;
using UnityEngine;

namespace StreetFighter.View
{
    /// <summary>
    /// 一个带 <see cref="SpriteRenderer"/> 的 GameObject，负责把原版的
    /// (left, top, width, height, direction, zoom) 画布坐标映射成 Unity 世界坐标。
    /// 原版画布 900x490，y 向下；这里把世界原点放在画布中心，1 世界单位 = 1 像素。
    /// </summary>
    public class SpriteView
    {
        protected readonly GameObject GameObject;
        protected readonly SpriteRenderer Renderer;

        public SpriteView(string name, int sortingOrder)
        {
            GameObject = new GameObject(name);
            Renderer = GameObject.AddComponent<SpriteRenderer>();
            Renderer.sortingOrder = sortingOrder;
        }

        public void SetVisible(bool visible)
        {
            if (GameObject.activeSelf != visible)
            {
                GameObject.SetActive(visible);
            }
        }

        /// <summary>按画布像素坐标显示一帧。</summary>
        /// <param name="background">图集名。</param>
        /// <param name="frame">帧号。</param>
        /// <param name="sourceFrameCount">图集的原始帧数，用于切片。</param>
        /// <param name="left">包围盒 left（画布像素）。</param>
        /// <param name="top">包围盒 top（画布像素）。</param>
        /// <param name="sourceWidth">单帧源宽（像素），用于决定翻转后的包围盒。</param>
        /// <param name="sourceHeight">单帧源高（像素）。</param>
        /// <param name="direction">朝向，1 朝右、-1 朝左。</param>
        /// <param name="zoom">缩放。</param>
        /// <param name="fixedHeight">为 true 时按 sourceHeight 切片（特效图使用）。</param>
        public void Show(string background, int frame, int sourceFrameCount, float left, float top,
            float sourceWidth, float sourceHeight, int direction, float zoom, bool fixedHeight = false)
        {
            if (string.IsNullOrEmpty(background))
            {
                return;
            }

            int frames = sourceFrameCount <= 0 ? 1 : sourceFrameCount;
            int index = Mathf.Clamp(frame, 0, frames - 1);

            if (fixedHeight)
            {
                Renderer.sprite = SpriteLibrary.GetSlice(background, index, frames, sourceHeight);
            }
            else
            {
                var sheet = SpriteLibrary.GetSheet(background, frames);
                if (sheet == null)
                {
                    return;
                }

                Renderer.sprite = sheet[index];
            }

            // 翻转时把原点放到包围盒右侧，保证 [left, left + sourceWidth * zoom] 不变
            float originX = direction == 1 ? left : left + sourceWidth * zoom;
            GameObject.transform.position = new Vector3(originX - GameConfig.MapWidth / 2f, GameConfig.MapHeight / 2f - top, 0f);
            GameObject.transform.localScale = new Vector3(zoom * direction, zoom, 1f);
        }

        /// <summary>背景层：整张图拉伸到指定像素尺寸。</summary>
        public void ShowStretched(string background, float left, float top, float width, float height)
        {
            var texture = SpriteLibrary.GetTexture(background);
            if (texture == null)
            {
                return;
            }

            Renderer.sprite = SpriteLibrary.GetWhole(background);
            GameObject.transform.position = new Vector3(left - GameConfig.MapWidth / 2f, GameConfig.MapHeight / 2f - top, 0f);
            GameObject.transform.localScale = new Vector3(width / texture.width, height / texture.height, 1f);
        }
    }
}
