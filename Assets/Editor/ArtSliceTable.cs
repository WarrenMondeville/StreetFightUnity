using System.Collections.Generic;
using StreetFighter.Config;

namespace StreetFighter.Editor
{
    /// <summary>
    /// 图集切片表：图名 → 帧数。数据全部来自配置资产：
    /// 角色序列帧取 <see cref="FighterAsset.States"/> 与 <see cref="FighterAsset.Combos"/> 的图集名 / 帧数，
    /// 特效图取 <see cref="GameSettingsAsset.HitEffects"/>。
    /// </summary>
    public static class ArtSliceTable
    {
        /// <summary>读取配置资产，生成「图名 → 帧数」表。</summary>
        public static Dictionary<string, int> Build()
        {
            var table = new Dictionary<string, int>();

            var fighters = ConfigAssets.LoadFighters();
            for (int i = 0; i < fighters.Length; i++)
            {
                AddAll(table, fighters[i].States);
                AddAll(table, fighters[i].Combos);
            }

            var settings = ConfigAssets.LoadSettings();
            if (settings != null)
            {
                var effects = settings.HitEffects;
                for (int i = 0; i < effects.Count; i++)
                {
                    Add(table, effects[i].Type, effects[i].FrameCount);
                }
            }

            return table;
        }

        private static void AddAll(Dictionary<string, int> table, IReadOnlyList<StateConfig> states)
        {
            for (int i = 0; i < states.Count; i++)
            {
                Add(table, states[i].Background, states[i].FrameCount);
            }
        }

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
