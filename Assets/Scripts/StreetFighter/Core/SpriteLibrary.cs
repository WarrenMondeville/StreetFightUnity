using System.Collections.Generic;
using UnityEngine;

namespace StreetFighter.Core
{
    /// <summary>
    /// 序列帧资源池。原版所有角色 / 特效图都是横向排列的 gif 条带，
    /// 这里已转成 png，运行时按 framesNum 横向切片成 Sprite。
    /// 切片使用 pivot(0, 1)（左上角）+ PPU=1，方便直接沿用原版 left / top 像素坐标。
    /// </summary>
    public static class SpriteLibrary
    {
        private const string ArtFolder = "Art";
        private const string ArtPathPrefix = "Art/";

        private static readonly Dictionary<string, Texture2D> Textures = new Dictionary<string, Texture2D>();
        private static readonly Dictionary<string, Sprite[]> Sheets = new Dictionary<string, Sprite[]>();
        private static readonly Dictionary<string, Sprite> WholeSprites = new Dictionary<string, Sprite>();
        private static readonly Dictionary<string, Sprite> Slices = new Dictionary<string, Sprite>();

        /// <summary>预热：把 Resources/Art 下的纹理一次性载入缓存。</summary>
        public static void Initialize()
        {
            var all = Resources.LoadAll<Texture2D>(ArtFolder);
            for (int i = 0; i < all.Length; i++)
            {
                Textures[all[i].name] = all[i];
            }
        }

        /// <summary>取原始纹理，找不到时告警并返回 null。</summary>
        public static Texture2D GetTexture(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return null;
            }

            Texture2D texture;
            if (Textures.TryGetValue(name, out texture))
            {
                return texture;
            }

            texture = Resources.Load<Texture2D>(ArtPathPrefix + name);
            if (texture == null)
            {
                Debug.LogWarning($"[SF] 找不到图片资源: {name}");
                return null;
            }

            Textures[name] = texture;
            return texture;
        }

        /// <summary>纹理像素尺寸，找不到返回 <see cref="Vector2.zero"/>。</summary>
        public static Vector2 GetSize(string name)
        {
            var texture = GetTexture(name);
            return texture == null ? Vector2.zero : new Vector2(texture.width, texture.height);
        }

        /// <summary>把条带横向切成 frames 张 Sprite。</summary>
        public static Sprite[] GetSheet(string name, int frames)
        {
            if (frames <= 0)
            {
                frames = 1;
            }

            string key = $"{name}|{frames}";
            Sprite[] sheet;
            if (Sheets.TryGetValue(key, out sheet))
            {
                return sheet;
            }

            var texture = GetTexture(name);
            if (texture == null)
            {
                return null;
            }

            sheet = new Sprite[frames];
            float width = texture.width / (float)frames;
            for (int i = 0; i < frames; i++)
            {
                var rect = new Rect(i * width, 0f, width, texture.height);
                sheet[i] = CreateSprite(texture, rect);
            }

            Sheets[key] = sheet;
            return sheet;
        }

        /// <summary>整张纹理作为一个 Sprite（背景层使用）。</summary>
        public static Sprite GetWhole(string name)
        {
            Sprite sprite;
            if (WholeSprites.TryGetValue(name, out sprite))
            {
                return sprite;
            }

            var texture = GetTexture(name);
            if (texture == null)
            {
                return null;
            }

            sprite = CreateSprite(texture, new Rect(0f, 0f, texture.width, texture.height));
            WholeSprites[name] = sprite;
            return sprite;
        }

        /// <summary>按源矩形切片（特效图使用固定高度，与原版一致）。</summary>
        public static Sprite GetSlice(string name, int frame, int frames, float height)
        {
            string key = $"{name}|{frames}|{frame}|{height}";
            Sprite sprite;
            if (Slices.TryGetValue(key, out sprite))
            {
                return sprite;
            }

            var texture = GetTexture(name);
            if (texture == null)
            {
                return null;
            }

            float width = texture.width / (float)frames;
            sprite = CreateSprite(texture, new Rect(frame * width, 0f, width, height));
            Slices[key] = sprite;
            return sprite;
        }

        private static Sprite CreateSprite(Texture2D texture, Rect rect) =>
            Sprite.Create(texture, rect, new Vector2(0f, 1f), 1f, 0u, SpriteMeshType.FullRect);
    }
}
