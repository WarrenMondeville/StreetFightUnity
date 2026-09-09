using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace StreetFighter.Editor
{
    /// <summary>
    /// 把 <c>Assets/Resources/Art</c> 下的条带图在导入期切成 Sprite 子资源，
    /// 帧数来自 <see cref="ArtSliceTable"/>（即 Config）。切片规则与运行时一致：
    /// 横向均分、pivot 左上角、PPU=1、FullRect。
    /// </summary>
    public static class ArtSpriteSlicer
    {
        private const string MenuPath = "StreetFighter/Slice Art Sprites";
        private const string ArtSearchFolder = "Assets/Resources/Art";

        private static readonly Vector2 Pivot = new Vector2(0f, 1f);

        /// <summary>对全部美术图重新应用导入设置并生成切片。</summary>
        [MenuItem(MenuPath)]
        public static void ApplyAll()
        {
            var table = ArtSliceTable.Build();
            var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { ArtSearchFolder });

            int sliced = 0;
            for (int i = 0; i < guids.Length; i++)
            {
                if (ApplyAt(AssetDatabase.GUIDToAssetPath(guids[i]), table))
                {
                    sliced++;
                }
            }

            if (sliced > 0)
            {
                AssetDatabase.SaveAssets();
            }

            Debug.Log($"[SF] 序列帧切片完成: 共 {guids.Length} 张，更新 {sliced} 张");
        }

        /// <summary>对单张图应用切片，返回切片数据是否发生变化。</summary>
        public static bool ApplyAt(string path, Dictionary<string, int> table)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (importer == null || texture == null)
            {
                return false;
            }

            int frames;
            if (!table.TryGetValue(texture.name, out frames))
            {
                frames = 1;
            }

            TextureSettings.Apply(importer);

            bool sliced = Apply(importer, texture, frames);
            if (!sliced)
            {
                // 切片没变化也要落盘，保证上面的导入设置生效。
                importer.SaveAndReimport();
            }

            return sliced;
        }

        /// <summary>写入切片数据；与现有设置一致时不重复导入。</summary>
        public static bool Apply(TextureImporter importer, Texture2D texture, int frames)
        {
            if (frames <= 1)
            {
                if (importer.spriteImportMode == SpriteImportMode.Single)
                {
                    return false;
                }

                importer.spriteImportMode = SpriteImportMode.Single;
                importer.SaveAndReimport();
                return true;
            }

            var expected = Build(texture, frames);
#pragma warning disable CS0618 // Unity 2022 仍可用；换成数据接口在 batchmode 下落不了盘
            var current = importer.spritesheet;

            if (importer.spriteImportMode == SpriteImportMode.Multiple && Same(current, expected))
            {
                return false;
            }

            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritesheet = expected;
#pragma warning restore CS0618
            importer.SaveAndReimport();
            return true;
        }

        private static SpriteMetaData[] Build(Texture2D texture, int frames)
        {
            var sheet = new SpriteMetaData[frames];
            float width = texture.width / (float)frames;

            for (int i = 0; i < frames; i++)
            {
                sheet[i] = new SpriteMetaData
                {
                    name = $"{texture.name}_{i}",
                    rect = new Rect(i * width, 0f, width, texture.height),
                    alignment = (int)SpriteAlignment.TopLeft,
                    pivot = Pivot,
                    border = Vector4.zero,
                };
            }

            return sheet;
        }

        private static bool Same(SpriteMetaData[] current, SpriteMetaData[] expected)
        {
            if (current == null || current.Length != expected.Length)
            {
                return false;
            }

            for (int i = 0; i < current.Length; i++)
            {
                if (current[i].name != expected[i].name ||
                    current[i].rect != expected[i].rect ||
                    current[i].pivot != expected[i].pivot ||
                    current[i].alignment != expected[i].alignment)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
