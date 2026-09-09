using System.Collections.Generic;
using UnityEngine;

namespace StreetFighter.Config
{
    /// <summary>
    /// 动作表资产：动作名 → 状态序列 + 锁级别。
    /// 放在 <c>Resources/Config</c> 下，运行时由 <see cref="ConfigAssets"/> 载入。
    /// </summary>
    [CreateAssetMenu(menuName = "StreetFighter/Play Table", fileName = "Play")]
    public sealed class PlayAsset : ScriptableObject
    {
        [SerializeField] private List<PlayActionConfig> _actions = new List<PlayActionConfig>();

        [System.NonSerialized] private Dictionary<string, PlayActionConfig> _map;

        /// <summary>全部动作。</summary>
        public IReadOnlyList<PlayActionConfig> Actions => _actions;

        /// <summary>按动作名查找，不存在时返回 null。</summary>
        public PlayActionConfig Get(string name)
        {
            if (name == null)
            {
                return null;
            }

            if (_map == null)
            {
                _map = new Dictionary<string, PlayActionConfig>();
                for (int i = 0; i < _actions.Count; i++)
                {
                    _map[_actions[i].Name] = _actions[i];
                }
            }

            PlayActionConfig action;
            return _map.TryGetValue(name, out action) ? action : null;
        }
    }
}
