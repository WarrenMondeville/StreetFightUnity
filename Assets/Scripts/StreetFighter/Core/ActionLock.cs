namespace StreetFighter.Core
{
    /// <summary>
    /// 动作优先级锁（等价于原版 interface.js 的 Lock）。
    /// 级别越大优先级越高；只有更高或同级别解锁时才能打断当前动作。
    /// </summary>
    public sealed class ActionLock
    {
        private int _level;

        /// <summary>当前锁级别，0 表示未锁定。</summary>
        public int Level => _level;

        /// <summary>是否处于锁定状态。</summary>
        public bool IsLocked => _level > 0;

        /// <summary>设置锁级别。</summary>
        public void Set(int level) => _level = level;
    }
}
