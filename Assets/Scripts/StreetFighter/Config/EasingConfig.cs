using StreetFighter.Core;
using UnityEngine;

namespace StreetFighter.Config
{
    /// <summary>
    /// 状态的位移与推进参数：横向位移 dx、纵向位移 top、推进间隔 step（同时也是运动时长系数）、缓动函数名。
    /// <c>autoTop</c> 为 true 时忽略 top，由运行时按包围盒自动计算（配置里对应 top 为 null）。
    /// </summary>
    [System.Serializable]
    public sealed class EasingConfig
    {
        [SerializeField] private float _dx;
        [SerializeField] private bool _autoTop;
        [SerializeField] private float _top;
        [SerializeField] private float _step = 3f;
        [SerializeField] private string _ease = EasingNames.Linear;

        /// <summary>横向位移（像素，实际使用时再乘朝向）。</summary>
        public float Dx => _dx;

        /// <summary>top 是否需要运行时自动计算。</summary>
        public bool AutoTop => _autoTop;

        /// <summary>纵向位移（像素）。</summary>
        public float Top => _top;

        /// <summary>每隔几个逻辑帧推进一帧，同时作为运动时长系数。</summary>
        public float Step => _step;

        /// <summary>缓动函数名，见 <c>EasingNames</c>。</summary>
        public string Ease => _ease;
    }
}
