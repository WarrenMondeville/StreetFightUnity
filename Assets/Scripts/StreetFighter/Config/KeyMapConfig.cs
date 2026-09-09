using System.Collections.Generic;
using UnityEngine;

namespace StreetFighter.Config
{
    /// <summary>
    /// 角色的按键表：令牌映射、移动表（含镜像表，前后方向互换）与普攻 / 必杀出招表。
    /// </summary>
    [System.Serializable]
    public sealed class KeyMapConfig
    {
        [SerializeField] private List<KeyCodeMapping> _mappings = new List<KeyCodeMapping>();
        [SerializeField] private List<TokenMapping> _moves = new List<TokenMapping>();
        [SerializeField] private List<TokenMapping> _movesMirrored = new List<TokenMapping>();
        [SerializeField] private List<TokenMapping> _normalAttacks = new List<TokenMapping>();
        [SerializeField] private List<TokenMapping> _specialAttacks = new List<TokenMapping>();

        /// <summary>keyCode → 令牌。</summary>
        public IReadOnlyList<KeyCodeMapping> Mappings => _mappings;

        /// <summary>令牌组合 → 移动动作（朝向为右时使用）。</summary>
        public IReadOnlyList<TokenMapping> Moves => _moves;

        /// <summary>令牌组合 → 移动动作（朝向为左时使用）。</summary>
        public IReadOnlyList<TokenMapping> MovesMirrored => _movesMirrored;

        /// <summary>单键 → 普攻动作。</summary>
        public IReadOnlyList<TokenMapping> NormalAttacks => _normalAttacks;

        /// <summary>方向序列 + 普攻 → 必杀动作。</summary>
        public IReadOnlyList<TokenMapping> SpecialAttacks => _specialAttacks;
    }
}
