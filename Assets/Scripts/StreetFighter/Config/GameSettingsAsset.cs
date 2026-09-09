using System.Collections.Generic;
using UnityEngine;

namespace StreetFighter.Config
{
    /// <summary>
    /// 全局配置资产：逻辑帧时长、输入采样周期、舞台参数、角色阴影与命中特效表。
    /// 放在 <c>Resources/Config</c> 下，运行时由 <see cref="ConfigAssets"/> 载入。
    /// </summary>
    [CreateAssetMenu(menuName = "StreetFighter/Game Settings", fileName = "Global")]
    public sealed class GameSettingsAsset : ScriptableObject
    {
        [SerializeField] private float _fps = 17f;
        [SerializeField] private float _keyFps = 800f;
        [SerializeField] private MapConfig _map = new MapConfig();
        [SerializeField] private string _spiritShadow = "fighterShadow";
        [SerializeField] private List<HitEffectConfig> _hitEffects = new List<HitEffectConfig>();

        /// <summary>逻辑帧时长（毫秒）。</summary>
        public float Fps => _fps;

        /// <summary>输入采样周期（毫秒）。</summary>
        public float KeyFps => _keyFps;

        /// <summary>舞台参数。</summary>
        public MapConfig Map => _map;

        /// <summary>角色脚下阴影图名。</summary>
        public string SpiritShadow => _spiritShadow;

        /// <summary>全部命中特效配置。</summary>
        public IReadOnlyList<HitEffectConfig> HitEffects => _hitEffects;

        /// <summary>按类型查找命中特效，缺失或帧数非法时返回 null。</summary>
        public HitEffectConfig GetHitEffect(string type)
        {
            for (int i = 0; i < _hitEffects.Count; i++)
            {
                if (_hitEffects[i].Type == type)
                {
                    return _hitEffects[i];
                }
            }

            return null;
        }
    }
}
