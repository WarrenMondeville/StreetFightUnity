using UnityEditor;
using UnityEngine;

namespace StreetFighter.Editor
{
    /// <summary>
    /// 序列帧资源导入设置：点采样 + 不压缩 + 保留透明通道，保证像素风格不糊。
    /// 只对 <c>Assets/Resources/Art</c> 下的纹理生效。
    /// 切片（Sprite 子资源）由 <see cref="ArtSpriteSlicer"/> 负责，这里不改动 spriteImportMode / 切片数据。
    /// </summary>
    public sealed class TextureSettings : AssetPostprocessor
    {
        private const string ArtMarker = "/Resources/Art/";

        private const int MaxTextureSize = 2048;

        private void OnPreprocessTexture()
        {
            if (!assetPath.Replace("\\", "/").Contains(ArtMarker))
            {
                return;
            }

            Apply((TextureImporter)assetImporter);
        }

        public static void Apply(TextureImporter importer)
        {
            // 切换 textureType 会清空切片，只在类型还没设成 Sprite 时改。
            if (importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
            }

            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = MaxTextureSize;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spritePixelsPerUnit = 1f;
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteAlignment = (int)SpriteAlignment.TopLeft;
            settings.spritePivot = new Vector2(0f, 1f);
            importer.SetTextureSettings(settings);
        }
    }
}
