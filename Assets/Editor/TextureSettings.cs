using UnityEditor;
using UnityEngine;

namespace StreetFighter.EditorTools
{
    /// <summary>
    /// 序列帧资源导入设置：点采样 + 不压缩 + 保留透明通道，保证像素风格不糊。
    /// </summary>
    public class TextureSettings : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            if (!assetPath.Replace("\\", "/").Contains("/Resources/Art/")) return;
            Apply((TextureImporter)assetImporter);
        }

        public static void Apply(TextureImporter imp)
        {
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.filterMode = FilterMode.Point;
            imp.mipmapEnabled = false;
            imp.alphaIsTransparency = true;
            imp.wrapMode = TextureWrapMode.Clamp;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.maxTextureSize = 2048;
            imp.spritePivot = new Vector2(0f, 1f);
            imp.spritePixelsPerUnit = 1f;
        }
    }
}
