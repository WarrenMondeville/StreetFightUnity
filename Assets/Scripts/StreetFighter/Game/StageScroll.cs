using UnityEngine;

namespace StreetFighter.Game
{
    /// <summary>共享的舞台横向卷轴位置。</summary>
    public sealed class StageScroll
    {
        /// <summary>背景内容总宽（比画布宽，多出来的部分靠滚动展示）。</summary>
        public const float ContentWidth = 1400f;

        public const float MinScrollLeft = 0f;
        public const float MaxScrollLeft = 500f;

        /// <summary>初始卷轴位置。</summary>
        public float ScrollLeft { get; private set; } = 250f;

        public void Add(float delta) => ScrollLeft = Mathf.Clamp(ScrollLeft + delta, MinScrollLeft, MaxScrollLeft);
    }
}
