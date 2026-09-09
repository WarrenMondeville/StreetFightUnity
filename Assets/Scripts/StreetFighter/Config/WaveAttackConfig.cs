using UnityEngine;

namespace StreetFighter.Config
{
    /// <summary>
    /// 飞行道具的判定配置。与近身攻击的区别是没有位移参数，改为判定体尺寸，
    /// 并且多一项 <c>defenseDamage</c>（对方防御时削减的血量）。
    /// </summary>
    [System.Serializable]
    public sealed class WaveAttackConfig
    {
        [SerializeField] private float _sizeOffsetX;
        [SerializeField] private float _sizeOffsetY;
        [SerializeField] private float _colliderSize;
        [SerializeField] private string _disappearEffect;
        [SerializeField] private string _beatState;
        [SerializeField] private float _damage;
        [SerializeField] private float _defenseDamage;

        /// <summary>判定体尺寸的横向偏移。</summary>
        public float SizeOffsetX => _sizeOffsetX;

        /// <summary>判定体尺寸的纵向偏移。</summary>
        public float SizeOffsetY => _sizeOffsetY;

        /// <summary>判定体尺寸。</summary>
        public float ColliderSize => _colliderSize;

        /// <summary>命中 / 相消时播放的特效类型。</summary>
        public string DisappearEffect => _disappearEffect;

        /// <summary>命中后对方进入的状态。</summary>
        public string BeatState => _beatState;

        /// <summary>伤害值。</summary>
        public float Damage => _damage;

        /// <summary>对方防御成功时削减的血量。</summary>
        public float DefenseDamage => _defenseDamage;
    }
}
