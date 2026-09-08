namespace StreetFighter.Core
{
    /// <summary>
    /// 全局事件名常量。原版用裸字符串当事件键，这里集中成常量避免拼写错误。
    /// </summary>
    public static class GameEvents
    {
        /// <summary>序列帧推进了一帧。</summary>
        public const string FrameStart = "frameStart";

        /// <summary>序列帧推进完成。</summary>
        public const string FrameDone = "frameDone";

        /// <summary>一段序列帧开始播放。</summary>
        public const string FramesStart = "framesStart";

        /// <summary>一段序列帧播放结束。</summary>
        public const string FramesDone = "framesDone";

        /// <summary>角色开始播放某个动作。</summary>
        public const string PlayStart = "playStart";

        /// <summary>取消对对手的推挤。</summary>
        public const string StopPush = "stopPush";

        /// <summary>判定体重叠。</summary>
        public const string Affirm = "affirm";

        /// <summary>判定体不再重叠。</summary>
        public const string UnAffirm = "unAffirm";

        /// <summary>血量条见底。</summary>
        public const string Empty = "empty";
    }
}
