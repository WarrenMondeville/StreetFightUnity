using UnityEditor;
using UnityEngine;

namespace StreetFighter.Editor
{
    /// <summary>
    /// 序列帧资源导入设置：点采样 + 不压缩 + 保留透明通道，保证像素风格不糊。
    /// 只对 <c>Assets/Resources/Art</c> 下的纹理生效。
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
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = MaxTextureSize;
            importer.spritePivot = new Vector2(0f, 1f);
            importer.spritePixelsPerUnit = 1f;
        }
    }
}
