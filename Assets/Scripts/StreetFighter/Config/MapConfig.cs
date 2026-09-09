using UnityEngine;

namespace StreetFighter.Config
{
    /// <summary>
    /// 舞台参数：远近两层背景图名、逻辑画布尺寸与角色缩放。
    /// </summary>
    [System.Serializable]
    public sealed class MapConfig
    {
        [SerializeField] private string _backgroundBehind = "behind";
        [SerializeField] private string _backgroundFront = "front";
        [SerializeField] private float _width = 776f;
        [SerializeField] private float _height = 440f;
        [SerializeField] private float _windowWidth = 776f;
        [SerializeField] private float _spiritZoom = 2.1f;

        /// <summary>远景背景图名。</summary>
        public string BackgroundBehind => _backgroundBehind;

        /// <summary>近景背景图名。</summary>
        public string BackgroundFront => _backgroundFront;

        /// <summary>舞台宽（像素）。</summary>
        public float Width => _width;

        /// <summary>舞台高（像素）。</summary>
        public float Height => _height;

        /// <summary>可视窗口宽（像素）。</summary>
        public float WindowWidth => _windowWidth;

        /// <summary>角色缩放。</summary>
        public float SpiritZoom => _spiritZoom;
    }
}
