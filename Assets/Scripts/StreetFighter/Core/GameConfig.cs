using System.Collections.Generic;
using StreetFighter.Config;
using UnityEngine;

namespace StreetFighter.Core
{
    /// <summary>
    /// 游戏配置的只读视图。数据来自 <c>Resources/Config</c> 下的配置资产（见 <see cref="ConfigAssets"/>），
    /// 玩法数值一律不写在 C# 里，改数值只需要改资产。
    /// </summary>
    public static class GameConfig
    {
        /// <summary>原版画布宽（像素）。</summary>
        public const float MapWidth = 900f;

        /// <summary>原版画布高（像素）。</summary>
        public const float MapHeight = 490f;

        private const float DefaultFps = 17f;
        private const float DefaultZoom = 2.1f;
        private const int DefaultEffectFrameCount = 3;
        private const float DefaultEffectHeight = 19f;

        private static readonly Dictionary<string, FighterAsset> Fighters = new Dictionary<string, FighterAsset>();

        /// <summary>已经报过缺失的状态，避免每帧刷同一条告警。</summary>
        private static readonly HashSet<string> MissingStates = new HashSet<string>();

        /// <summary>全局设置资产。</summary>
        public static GameSettingsAsset Settings { get; private set; }

        /// <summary>动作表资产。</summary>
        public static PlayAsset Play { get; private set; }

        /// <summary>逻辑帧时长（毫秒）。</summary>
        public static float Fps { get; private set; } = DefaultFps;

        /// <summary>输入采样周期（毫秒）。</summary>
        public static float KeyFps { get; private set; }

        /// <summary>角色缩放。</summary>
        public static float Zoom { get; private set; } = DefaultZoom;

        /// <summary>远景背景图名。</summary>
        public static string BackgroundBehind { get; private set; }

        /// <summary>近景背景图名。</summary>
        public static string BackgroundFront { get; private set; }

        /// <summary>角色脚下阴影图名。</summary>
        public static string SpiritShadow { get; private set; }

        /// <summary>载入全部配置资产。</summary>
        public static void Load()
        {
            Settings = ConfigAssets.LoadSettings();
            Play = ConfigAssets.LoadPlay();

            Fighters.Clear();
            var fighters = ConfigAssets.LoadFighters();
            for (int i = 0; i < fighters.Length; i++)
            {
                Fighters[fighters[i].FighterName] = fighters[i];
            }

            if (Settings == null)
            {
                Debug.LogWarning($"[SF] 找不到全局配置资产: Resources/{ConfigAssets.RootFolder}/Global");
                return;
            }

            Fps = Settings.Fps > 0f ? Settings.Fps : DefaultFps;
            KeyFps = Settings.KeyFps;

            var map = Settings.Map;
            if (map != null)
            {
                Zoom = map.SpiritZoom > 0f ? map.SpiritZoom : DefaultZoom;
                BackgroundBehind = map.BackgroundBehind;
                BackgroundFront = map.BackgroundFront;
            }

            SpiritShadow = Settings.SpiritShadow;
        }

        /// <summary>按角色键取配置资产，不存在时返回 null。</summary>
        public static FighterAsset GetSpirit(string key)
        {
            if (key == null)
            {
                return null;
            }

            FighterAsset fighter;
            if (Fighters.TryGetValue(key, out fighter))
            {
                return fighter;
            }

            Debug.LogWarning($"[SF] 找不到角色配置资产: {key}");
            return null;
        }

        /// <summary>动作表里的一个动作，不存在时返回 null。</summary>
        public static PlayActionConfig GetPlay(string state)
        {
            var action = Play?.Get(state);
            if (action == null && state != null && MissingStates.Add(state))
            {
                Debug.LogWarning($"[SF] 动作表缺少状态: {state}");
            }

            return action;
        }

        /// <summary>动作的优先级锁级别，动作不存在时为 0。</summary>
        public static int GetPlayLock(string state)
        {
            var action = Play?.Get(state);
            return action == null ? 0 : action.LockLevel;
        }

        /// <summary>特效图的帧数。</summary>
        public static int GetEffectFrameCount(string type)
        {
            var effect = Settings?.GetHitEffect(type);
            return effect == null || effect.FrameCount <= 0 ? DefaultEffectFrameCount : effect.FrameCount;
        }

        /// <summary>特效图的绘制高度。</summary>
        public static float GetEffectHeight(string type)
        {
            var effect = Settings?.GetHitEffect(type);
            return effect == null || effect.Height <= 0f ? DefaultEffectHeight : effect.Height;
        }
    }
}
