using StreetFighter.Core;
using UnityEngine;

namespace StreetFighter.Config
{
    /// <summary>
    /// 近身攻击的判定配置。判定体从角色身上 <c>offset</c> 处生成，随后按 <c>move</c> / <c>duration</c> 位移，
    /// 命中后播放 <c>effect</c> 特效并把对方打成 <c>beatState</c>。
    /// </summary>
    [System.Serializable]
    public sealed class MeleeAttackConfig
    {
        [SerializeField] private float _offsetX;
        [SerializeField] private float _offsetY;
        [SerializeField] private float _moveX;
        [SerializeField] private float _moveY;
        [SerializeField] private float _duration = 100f;
        [SerializeField] private string _ease = EasingNames.Linear;
        [SerializeField] private string _effect;
        [SerializeField] private string _beatState;
        [SerializeField] private float _damage;

        /// <summary>判定体相对角色左边的横向偏移。</summary>
        public float OffsetX => _offsetX;

        /// <summary>判定体相对角色顶边的纵向偏移。</summary>
        public float OffsetY => _offsetY;

        /// <summary>判定体的横向位移（像素，实际使用时再乘朝向）。</summary>
        public float MoveX => _moveX;

        /// <summary>判定体的纵向位移（像素）。</summary>
        public float MoveY => _moveY;

        /// <summary>位移时长（毫秒）。</summary>
        public float Duration => _duration;

        /// <summary>位移缓动函数名。</summary>
        public string Ease => _ease;

        /// <summary>命中特效类型。</summary>
        public string Effect => _effect;

        /// <summary>命中后对方进入的状态。</summary>
        public string BeatState => _beatState;

        /// <summary>伤害值。</summary>
        public float Damage => _damage;
    }
}
