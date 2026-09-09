using UnityEngine;

namespace StreetFighter.Config
{
    /// <summary>
    /// 键盘 keyCode 到输入令牌的映射（如 65 → "a"）。顺序有意义：移动令牌的枚举顺序来自这张表。
    /// </summary>
    [System.Serializable]
    public sealed class KeyCodeMapping
    {
        [SerializeField] private int _keyCode;
        [SerializeField] private string _token;

        /// <summary>键盘 keyCode。</summary>
        public int KeyCode => _keyCode;

        /// <summary>输入令牌（单个字母）。</summary>
        public string Token => _token;
    }
}
