using System.Collections.Generic;
using UnityEngine;

namespace StreetFighter.Core
{
    /// <summary>
    /// 序列帧资源池。所有角色 / 特效图都是横向排列的条带图，
    /// 切片在导入期由 <c>StreetFighter → Slice Art Sprites</c> 写成 Sprite 子资源（见 Editor/ArtSpriteSlicer），
    /// 运行时只做查表；只有在没找到预切好的 Sprite 时才退回 <see cref="Sprite.Create"/>。
    /// 切片使用 pivot(0, 1)（左上角）+ PPU=1，方便直接沿用 left / top 像素坐标。
    /// </summary>
    public static class SpriteLibrary
    {
        private const string ArtFolder = "Art";
        private const string ArtPathPrefix = "Art/";

        private static readonly Dictionary<string, Texture2D> Textures = new Dictionary<string, Texture2D>();

        /// <summary>导入期切好的帧序列：图名 → 按 rect.x 从左到右排好的 Sprite。</summary>
        private static readonly Dictionary<string, Sprite[]> ImportedSheets = new Dictionary<string, Sprite[]>();

        /// <summary>整图 Sprite（单帧图 / 背景层）。</summary>
        private static readonly Dictionary<string, Sprite> WholeSprites = new Dictionary<string, Sprite>();

        private static readonly Dictionary<string, Sprite[]> Sheets = new Dictionary<string, Sprite[]>();
        private static readonly Dictionary<string, Sprite> Slices = new Dictionary<string, Sprite>();
        private static readonly HashSet<string> FallbackWarned = new HashSet<string>();

        /// <summary>预热：把 Resources/Art 下的纹理与预切好的 Sprite 一次性载入缓存。</summary>
        public static void Initialize()
        {
            var all = Resources.LoadAll<Texture2D>(ArtFolder);
            for (int i = 0; i < all.Length; i++)
            {
                Textures[all[i].name] = all[i];
            }

            LoadSprites();
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

        /// <summary>取预切好的帧序列；帧数对不上时返回 null。</summary>
        public static Sprite[] GetSheet(string name, int frames)
        {
            if (frames <= 0)
            {
                frames = 1;
            }

            Sprite[] imported;
            if (ImportedSheets.TryGetValue(name, out imported) && imported.Length == frames)
            {
                return imported;
            }

            if (frames == 1)
            {
                var whole = GetWhole(name);
                return whole == null ? null : new[] { whole };
            }

            return CreateSheet(name, frames);
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
            WarnFallback($"{name}|whole");
            return sprite;
        }

        /// <summary>按帧号取一帧（特效图用固定高度，与原版一致）。</summary>
        public static Sprite GetSlice(string name, int frame, int frames, float height)
        {
            if (frames <= 0)
            {
                frames = 1;
            }

            Sprite[] imported;
            if (ImportedSheets.TryGetValue(name, out imported) && imported.Length == frames)
            {
                var hit = imported[Mathf.Clamp(frame, 0, frames - 1)];
                if (Mathf.Approximately(hit.rect.height, height))
                {
                    return hit;
                }
            }

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
            WarnFallback(key);
            return sprite;
        }

        private static void LoadSprites()
        {
            var sprites = Resources.LoadAll<Sprite>(ArtFolder);
            var groups = new Dictionary<string, List<Sprite>>();

            for (int i = 0; i < sprites.Length; i++)
            {
                var sprite = sprites[i];
                if (sprite == null || sprite.texture == null)
                {
                    continue;
                }

                List<Sprite> group;
                if (!groups.TryGetValue(sprite.texture.name, out group))
                {
                    group = new List<Sprite>();
                    groups[sprite.texture.name] = group;
                }

                group.Add(sprite);
            }

            foreach (var pair in groups)
            {
                var group = pair.Value;
                group.Sort((a, b) => a.rect.x.CompareTo(b.rect.x));
                var sheet = group.ToArray();
                ImportedSheets[pair.Key] = sheet;

                if (sheet.Length == 1)
                {
                    WholeSprites[pair.Key] = sheet[0];
                }
            }
        }

        private static Sprite[] CreateSheet(string name, int frames)
        {
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
            WarnFallback(key);
            return sheet;
        }

        private static void WarnFallback(string key)
        {
            if (!FallbackWarned.Add(key))
            {
                return;
            }

            Debug.LogWarning($"[SF] 图片未预先切片，运行时创建 Sprite: {key}（请执行 StreetFighter → Slice Art Sprites）");
        }

        private static Sprite CreateSprite(Texture2D texture, Rect rect) =>
            Sprite.Create(texture, rect, new Vector2(0f, 1f), 1f, 0u, SpriteMeshType.FullRect);
    }
}
