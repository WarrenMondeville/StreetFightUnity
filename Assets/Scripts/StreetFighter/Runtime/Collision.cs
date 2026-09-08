using System.Collections.Generic;
using UnityEngine;

namespace StreetFighter
{
    /// <summary>参与碰撞检测的实体。</summary>
    public interface IBody
    {
        float Left { get; set; }
        float Top { get; set; }
        float Width { get; }
        float Height { get; }
        object Master { get; }
    }

    /// <summary>
    /// 复刻 interface.js 的 Collision：全局共享一个实体表，圆形相交判定，
    /// 半径 = (自身宽/2 + 对方宽/2) * spiritZoom。
    /// </summary>
    public class Collider
    {
        private static readonly List<IBody> Stack = new List<IBody>();

        private readonly IBody _self;
        private readonly float _rw, _rh;

        public readonly Evt Event = new Evt();

        /// <summary>最近一次命中的目标（供 affirm 回调读取）。</summary>
        public IBody Other;
        /// <summary>最近一次命中的方位：自身在目标的左/右。</summary>
        public string Dir;

        public Collider(IBody self, float w = 0, float h = 0)
        {
            _self = self;
            _rw = w;
            _rh = h;
            if (!Stack.Contains(self)) Stack.Add(self);
        }

        public static void Clear() => Stack.Clear();

        public void Check()
        {
            float width = _rw > 0 ? _rw : _self.Width;
            float height = _rh > 0 ? _rh : _self.Height;

            for (int i = 0; i < Stack.Count; i++)
            {
                var c = Stack[i];
                if (ReferenceEquals(c, _self)) continue;
                if (ReferenceEquals(c.Master, _self)) continue;
                if (ReferenceEquals(_self.Master, c)) continue;

                float der = (width / 2f + c.Width / 2f) * Cfg.Zoom;
                float x1 = _self.Left + width * Cfg.Zoom / 2f;
                float y1 = _self.Top + height * Cfg.Zoom / 2f;
                float x2 = c.Left + c.Width * Cfg.Zoom / 2f;
                float y2 = c.Top + c.Height * Cfg.Zoom / 2f;

                float x = Mathf.Abs(x2 - x1);
                float y = Mathf.Abs(y2 - y1);

                if (x * x + y * y <= der * der)
                {
                    Other = c;
                    Dir = _self.Left < c.Left ? "right" : "left";
                    Event.Fire("affirm");
                }
            }

            Event.Fire("unAffirm");
        }
    }
}
