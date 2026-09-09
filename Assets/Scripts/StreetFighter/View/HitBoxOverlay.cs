using StreetFighter.Core;
using StreetFighter.Gameplay;
using UnityEngine;

namespace StreetFighter.View
{
    /// <summary>
    /// 测试模式的判定框叠加层：受击框（角色本体）、近身攻击框、波动拳判定框。
    /// 用 <see cref="Gizmos"/> 绘制（Scene 视图可见，Game 视图需打开 Gizmos 开关），
    /// 尺寸直接取自 <see cref="BodyCollider"/>，与实际参与判定的尺寸一致。
    /// </summary>
    public sealed class HitBoxOverlay
    {
        /// <summary>填充透明度。</summary>
        private const float FillAlpha = 0.22f;

        /// <summary>厚度（世界单位），避免零深度导致的退化。</summary>
        private const float Depth = 0.01f;

        /// <summary>受击框颜色。</summary>
        private static readonly Color HurtColor = new Color(0.12f, 1f, 0.25f);

        /// <summary>近身攻击框颜色。</summary>
        private static readonly Color AttackColor = new Color(1f, 0.12f, 0.12f);

        /// <summary>波动拳判定框颜色。</summary>
        private static readonly Color WaveColor = new Color(1f, 0.78f, 0.08f);

        /// <summary>绘制所有角色的判定框；测试模式关闭时什么都不做。</summary>
        public void DrawGizmos(params Spirit[] spirits)
        {
            if (!DebugMode.Enabled)
            {
                return;
            }

            for (int i = 0; i < spirits.Length; i++)
            {
                Draw(spirits[i]);
            }
        }

        private static void Draw(Spirit spirit)
        {
            if (spirit?.Collider == null)
            {
                return;
            }

            DrawBox(spirit.Collider, HurtColor);

            if (spirit.Attack.IsActive)
            {
                DrawBox(spirit.Attack.Collider, AttackColor);
            }

            if (spirit.Wave.IsFiring)
            {
                DrawBox(spirit.Wave.Collider, WaveColor);
            }
        }

        private static void DrawBox(BodyCollider collider, Color color)
        {
            if (collider == null)
            {
                return;
            }

            float width = collider.BoxWidth * GameConfig.Zoom;
            float height = collider.BoxHeight * GameConfig.Zoom;

            // 画布坐标（left / top，y 向下）转成世界坐标，规则与 SpriteView 一致
            var center = new Vector3(
                collider.BoxLeft - GameConfig.MapWidth / 2f + width / 2f,
                GameConfig.MapHeight / 2f - collider.BoxTop - height / 2f,
                0f);

            var size = new Vector3(width, height, Depth);

            Gizmos.color = new Color(color.r, color.g, color.b, FillAlpha);
            Gizmos.DrawCube(center, size);

            Gizmos.color = color;
            Gizmos.DrawWireCube(center, size);
        }
    }
}
