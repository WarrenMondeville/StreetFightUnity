using System;
using System.Collections.Generic;
using StreetFighter.Core;
using UnityEngine;

namespace StreetFighter.Gameplay
{
    /// <summary>
    /// 碰撞检测：全局共享一个实体表，圆形相交判定，
    /// 半径 = (自身宽 / 2 + 对方宽 / 2) * spiritZoom。
    /// </summary>
    public sealed class BodyCollider
    {
        private static readonly List<ICollidable> Bodies = new List<ICollidable>();

        private readonly ICollidable _self;
        private readonly float _overrideWidth;
        private readonly float _overrideHeight;

        /// <summary>判定体重叠时派发，参数为对方实体与自身相对对方的方位。</summary>
        public event Action<ICollidable, Side> Hit;

        public BodyCollider(ICollidable self, float width = 0f, float height = 0f)
        {
            _self = self;
            _overrideWidth = width;
            _overrideHeight = height;

            if (!Bodies.Contains(self))
            {
                Bodies.Add(self);
            }
        }

        /// <summary>清空实体表（重开一局时调用）。</summary>
        public static void Clear() => Bodies.Clear();

        /// <summary>实际参与判定的宽度（构造时传入的覆盖值优先）。</summary>
        public float BoxWidth => _overrideWidth > 0f ? _overrideWidth : _self.Width;

        /// <summary>实际参与判定的高度（构造时传入的覆盖值优先）。</summary>
        public float BoxHeight => _overrideHeight > 0f ? _overrideHeight : _self.Height;

        /// <summary>判定框左上角的画布 x，与 <see cref="Check"/> 使用的判定中心对齐。</summary>
        public float BoxLeft => _self.Left + (_self.Width - BoxWidth) * GameConfig.Zoom / 2f;

        /// <summary>判定框左上角的画布 y，与 <see cref="Check"/> 使用的判定中心对齐。</summary>
        public float BoxTop => _self.Top + (_self.Height - BoxHeight) * GameConfig.Zoom / 2f;

        /// <summary>与实体表中的所有其它实体做一次检测。</summary>
        public void Check()
        {
            float width = BoxWidth;
            float height = BoxHeight;

            for (int i = 0; i < Bodies.Count; i++)
            {
                var other = Bodies[i];
                if (ReferenceEquals(other, _self))
                {
                    continue;
                }

                if (ReferenceEquals(other.Master, _self))
                {
                    continue;
                }

                if (ReferenceEquals(_self.Master, other))
                {
                    continue;
                }

                float radius = (width / 2f + other.Width / 2f) * GameConfig.Zoom;
                float x1 = _self.Left + width * GameConfig.Zoom / 2f;
                float y1 = _self.Top + height * GameConfig.Zoom / 2f;
                float x2 = other.Left + other.Width * GameConfig.Zoom / 2f;
                float y2 = other.Top + other.Height * GameConfig.Zoom / 2f;

                float dx = Mathf.Abs(x2 - x1);
                float dy = Mathf.Abs(y2 - y1);

                if (dx * dx + dy * dy <= radius * radius)
                {
                    Hit?.Invoke(other, _self.Left < other.Left ? Side.Right : Side.Left);
                }
            }
        }
    }
}
