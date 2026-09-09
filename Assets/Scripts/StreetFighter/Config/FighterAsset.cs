using System.Collections.Generic;
using UnityEngine;

namespace StreetFighter.Config
{
    /// <summary>
    /// 角色配置资产：默认状态、状态表、空中组合技表与按键表。
    /// 一个角色一份资产，放在 <c>Resources/Config/Fighters</c> 下即可被自动载入，不需要改代码。
    /// </summary>
    [CreateAssetMenu(menuName = "StreetFighter/Fighter", fileName = "Fighter")]
    public sealed class FighterAsset : ScriptableObject
    {
        [SerializeField] private string _fighterName;
        [SerializeField] private string _defaultState = "wait";
        [SerializeField] private List<StateConfig> _states = new List<StateConfig>();
        [SerializeField] private List<StateConfig> _combos = new List<StateConfig>();
        [SerializeField] private KeyMapConfig _keyMap = new KeyMapConfig();

        [System.NonSerialized] private Dictionary<string, StateConfig> _stateMap;
        [System.NonSerialized] private Dictionary<string, StateConfig> _comboMap;

        /// <summary>角色键，如 RYU1。</summary>
        public string FighterName => string.IsNullOrEmpty(_fighterName) ? name : _fighterName;

        /// <summary>默认状态名。</summary>
        public string DefaultState => _defaultState;

        /// <summary>全部状态。</summary>
        public IReadOnlyList<StateConfig> States => _states;

        /// <summary>全部空中组合技，键形如「跳状态_攻击」。</summary>
        public IReadOnlyList<StateConfig> Combos => _combos;

        /// <summary>按键表。</summary>
        public KeyMapConfig KeyMap => _keyMap;

        /// <summary>按状态名查找，不存在时返回 null。</summary>
        public StateConfig GetState(string stateName)
        {
            if (stateName == null)
            {
                return null;
            }

            if (_stateMap == null)
            {
                _stateMap = Build(_states);
            }

            StateConfig state;
            return _stateMap.TryGetValue(stateName, out state) ? state : null;
        }

        /// <summary>按「跳状态_攻击」查找空中组合技，不存在时返回 null。</summary>
        public StateConfig GetCombo(string comboName)
        {
            if (comboName == null)
            {
                return null;
            }

            if (_comboMap == null)
            {
                _comboMap = Build(_combos);
            }

            StateConfig state;
            return _comboMap.TryGetValue(comboName, out state) ? state : null;
        }

        private static Dictionary<string, StateConfig> Build(IReadOnlyList<StateConfig> source)
        {
            var map = new Dictionary<string, StateConfig>();
            for (int i = 0; i < source.Count; i++)
            {
                map[source[i].Name] = source[i];
            }

            return map;
        }
    }
}
