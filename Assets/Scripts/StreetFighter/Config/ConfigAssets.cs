using UnityEngine;

namespace StreetFighter.Config
{
    /// <summary>
    /// 配置资产的载入入口。全部配置都在 <c>Resources/Config</c> 下：
    /// 一份全局设置、一份动作表，以及 <c>Fighters</c> 目录下每个角色一份资产。
    /// 新增角色只要往 <c>Fighters</c> 里丢一份 <see cref="FighterAsset"/>。
    /// </summary>
    public static class ConfigAssets
    {
        /// <summary>配置资产所在目录（相对 Resources）。</summary>
        public const string RootFolder = "Config";

        /// <summary>角色资产所在目录（相对 Resources）。</summary>
        public const string FightersFolder = "Config/Fighters";

        private const string SettingsPath = "Config/Global";
        private const string PlayPath = "Config/Play";

        /// <summary>载入全局设置。</summary>
        public static GameSettingsAsset LoadSettings() => Resources.Load<GameSettingsAsset>(SettingsPath);

        /// <summary>载入动作表。</summary>
        public static PlayAsset LoadPlay() => Resources.Load<PlayAsset>(PlayPath);

        /// <summary>载入全部角色资产，按资产名排序。</summary>
        public static FighterAsset[] LoadFighters()
        {
            var fighters = Resources.LoadAll<FighterAsset>(FightersFolder);
            System.Array.Sort(fighters, (a, b) => string.CompareOrdinal(a.name, b.name));
            return fighters;
        }
    }
}
