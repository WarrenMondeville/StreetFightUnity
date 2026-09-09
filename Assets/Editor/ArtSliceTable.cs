using System.Collections.Generic;
using StreetFighter.Core;

namespace StreetFighter.Editor
{
    /// <summary>
    /// 图集切片表：图名 → 帧数。数据全部来自 Config：
    /// 角色序列帧取 <c>Spirit.*.states.*</c> 的 bg / framesNum，特效图取 <c>hitEffect.*</c> 的 framesNum。
    /// </summary>
    public static class ArtSliceTable
    {
        private const string SpiritKey = "Spirit";
        private const string StatesKey = "states";
        private const string BackgroundKey = "bg";
        private const string FrameCountKey = "framesNum";
        private const string HitEffectKey = "hitEffect";

        /// <summary>读取配置，生成「图名 → 帧数」表。</summary>
        public static Dictionary<string, int> Build()
        {
            var root = ConfigLoader.Load();
            var table = new Dictionary<string, int>();

            var spirits = Members(root.Get(SpiritKey));
            for (int i = 0; i < spirits.Count; i++)
            {
                var states = Members(spirits[i].Get(StatesKey));
                for (int j = 0; j < states.Count; j++)
                {
                    Add(table, states[j].Get(BackgroundKey).AsString, states[j].Get(FrameCountKey).AsInt);
                }
            }

            var effects = root.Get(HitEffectKey);
            if (effects.Keys != null)
            {
                for (int i = 0; i < effects.Keys.Count; i++)
                {
                    Add(table, effects.Keys[i], effects.Values[i].Get(FrameCountKey).AsInt);
                }
            }

            return table;
        }

        private static List<JVal> Members(JVal node) =>
            node.Values ?? new List<JVal>();

        private static void Add(Dictionary<string, int> table, string name, int frames)
        {
            if (string.IsNullOrEmpty(name) || frames <= 0)
            {
                return;
            }

            table[name] = frames;
        }
    }
}
