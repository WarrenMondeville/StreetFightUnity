using UnityEngine;

namespace StreetFighter.Config
{
    /// <summary>
    /// 输入令牌到动作名的映射。移动表里键是令牌组合（"wa"），必杀表里键是逗号连接的序列（"crouch,forward,light_boxing"）。
    /// </summary>
    [System.Serializable]
    public sealed class TokenMapping
    {
        [SerializeField] private string _token;
        [SerializeField] private string _state;

        /// <summary>令牌或令牌组合。</summary>
        public string Token => _token;

        /// <summary>对应的动作名。</summary>
        public string State => _state;
    }
}
