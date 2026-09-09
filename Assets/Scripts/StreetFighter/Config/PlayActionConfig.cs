using UnityEngine;

namespace StreetFighter.Config
{
    /// <summary>
    /// 一个可播放动作：由若干状态依次组成，并带一个优先级锁级别。
    /// lock：0 自由移动 / 1 跳跃 / 2 出招 / 3 受击 / 4 倒地类 / 5 死亡。
    /// </summary>
    [System.Serializable]
    public sealed class PlayActionConfig
    {
        [SerializeField] private string _name;
        [SerializeField] private string[] _compose;
        [SerializeField] private int _lockLevel;

        /// <summary>动作名。</summary>
        public string Name => _name;

        /// <summary>依次播放的状态名。</summary>
        public string[] Compose => _compose;

        /// <summary>优先级锁级别，数值越大越不容易被打断。</summary>
        public int LockLevel => _lockLevel;
    }
}
