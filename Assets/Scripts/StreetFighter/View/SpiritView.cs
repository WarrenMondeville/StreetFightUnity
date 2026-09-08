using StreetFighter.Core;
using StreetFighter.Gameplay;

namespace StreetFighter.View
{
    /// <summary>角色本体 + 脚下阴影。</summary>
    public sealed class SpiritView
    {
        private const float ShadowTop = 73f;

        private readonly SpriteView _body;
        private readonly SpriteView _shadow;

        public SpiritView(string key, int sortingOrder)
        {
            _body = new SpriteView(key, sortingOrder);
            _shadow = new SpriteView($"{key}_shadow", sortingOrder - 1);
        }

        public void Render(Spirit spirit, float zoom, int sortingOrder)
        {
            var frames = spirit.Frames;
            var size = SpriteLibrary.GetSize(frames.DrawBackground);
            int sourceFrames = frames.DrawSourceFrameCount <= 0 ? 1 : frames.DrawSourceFrameCount;

            _body.Show(frames.DrawBackground, frames.DrawFrame, sourceFrames, spirit.Left, spirit.Top,
                size.x / sourceFrames, size.y, frames.Direction, zoom);

            var shadowSize = SpriteLibrary.GetSize(GameConfig.SpiritShadow);
            _shadow.Show(GameConfig.SpiritShadow, 0, 1, spirit.ShadowLeft, GameConfig.MapHeight - ShadowTop,
                spirit.ShadowWidth, shadowSize.y, spirit.Direction, zoom);
        }
    }
}
