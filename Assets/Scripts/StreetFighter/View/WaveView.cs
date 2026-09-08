using System.Collections.Generic;

namespace StreetFighter.View
{
    /// <summary>波动拳飞行道具的视图（按角色键复用）。</summary>
    public static class WaveView
    {
        private const string ObjectNamePrefix = "wave_";

        private static readonly Dictionary<string, SpriteView> Views = new Dictionary<string, SpriteView>();

        public static SpriteView Get(string key, int sortingOrder)
        {
            SpriteView view;
            if (!Views.TryGetValue(key, out view))
            {
                view = new SpriteView(ObjectNamePrefix + sortingOrder, sortingOrder);
                Views[key] = view;
            }

            return view;
        }

        public static void Hide(string key)
        {
            SpriteView view;
            if (Views.TryGetValue(key, out view))
            {
                view.SetVisible(false);
            }
        }
    }
}
