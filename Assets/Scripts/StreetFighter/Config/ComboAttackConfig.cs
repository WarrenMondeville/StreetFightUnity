using UnityEngine;

namespace StreetFighter.Config
{
    /// <summary>
    /// 空中组合技的判定配置。判定体不独立位移，而是每帧贴在角色身上的 <c>offset</c> 处。
    /// </summary>
    [System.Serializable]
    public sealed class ComboAttackConfig
    {
        [SerializeField] private float _offsetX;
        [SerializeField] private float _offsetY;
        [SerializeField] private float _size = 50f;
        [SerializeField] private string _effect;
        [SerializeField] private string _beatState;
        [SerializeField] private float _damage;

        /// <summary>判定体相对角色左边的横向偏移。</summary>
        public float OffsetX => _offsetX;

        /// <summary>判定体相对角色顶边的纵向偏移。</summary>
        public float OffsetY => _offsetY;

        /// <summary>判定体边长。</summary>
        public float Size => _size;

        /// <summary>命中特效类型。</summary>
        public string Effect => _effect;

        /// <summary>命中后对方进入的状态。</summary>
        public string BeatState => _beatState;

        /// <summary>伤害值。</summary>
        public float Damage => _damage;
    }
}
