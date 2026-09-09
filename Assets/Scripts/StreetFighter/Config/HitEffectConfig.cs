using UnityEngine;

namespace StreetFighter.Config
{
    /// <summary>
    /// 一类命中特效的图集帧数与绘制高度。
    /// </summary>
    [System.Serializable]
    public sealed class HitEffectConfig
    {
        [SerializeField] private string _type;
        [SerializeField] private int _frameCount = 3;
        [SerializeField] private float _height = 19f;

        /// <summary>特效类型名，同时也是图集名。</summary>
        public string Type => _type;

        /// <summary>图集横向切片帧数。</summary>
        public int FrameCount => _frameCount;

        /// <summary>绘制高度（像素）。</summary>
        public float Height => _height;
    }
}
