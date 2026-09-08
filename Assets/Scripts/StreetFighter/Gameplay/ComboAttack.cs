using System;

namespace StreetFighter.Gameplay
{
    /// <summary>
    /// 空中组合技：在原动作上叠加播放另一段序列帧（等价于原版 SpiritFrames 里的 combo 支持）。
    /// </summary>
    public sealed class ComboAttack
    {
        private const int DefaultAfterFrame = 2;

        /// <summary>组合技图集名。</summary>
        public string Background { get; private set; }

        /// <summary>当前帧序号。</summary>
        public int CurrentFrame { get; private set; }

        /// <summary>总帧数（含重复帧展开）。</summary>
        public int FrameCount { get; private set; }

        /// <summary>原始帧数（用于切片）。</summary>
        public int SourceFrameCount { get; private set; } = 1;

        /// <summary>重复帧模式。</summary>
        public int[] RepeatPattern { get; private set; }

        /// <summary>结束后回退到的基础帧。</summary>
        public int AfterFrame { get; private set; } = DefaultAfterFrame;

        private bool _isPlaying;
        private Action _onFinished;

        /// <summary>开始播放；已经在播放时返回 false。</summary>
        public bool Start(string background, int frameCount, int[] repeatPattern, int afterFrame)
        {
            if (_isPlaying)
            {
                return false;
            }

            _isPlaying = true;
            Background = background;
            CurrentFrame = 0;
            FrameCount = frameCount <= 0 ? 1 : frameCount;
            SourceFrameCount = FrameCount;
            RepeatPattern = repeatPattern;
            AfterFrame = afterFrame > 0 ? afterFrame : DefaultAfterFrame;

            if (RepeatPattern != null)
            {
                int total = 0;
                for (int i = 0; i < RepeatPattern.Length; i++)
                {
                    total += RepeatPattern[i];
                }

                FrameCount = total;
            }

            return true;
        }

        /// <summary>
        /// 推进一帧；返回 null 表示当前没有组合技在播放。
        /// baseFrame 会被钳制到 <c>baseFrameCount - AfterFrame</c>（与原版一致）。
        /// </summary>
        public ComboAttack Next(ref int baseFrame, int baseFrameCount)
        {
            if (!_isPlaying)
            {
                return null;
            }

            if (CurrentFrame >= FrameCount)
            {
                if (baseFrame < baseFrameCount - AfterFrame)
                {
                    baseFrame = baseFrameCount - AfterFrame;
                }

                _onFinished?.Invoke();
            }

            if (CurrentFrame >= FrameCount)
            {
                return null;
            }

            CurrentFrame++;
            return this;
        }

        public void Stop()
        {
            Background = null;
            CurrentFrame = 0;
            FrameCount = 0;
            RepeatPattern = null;
            _isPlaying = false;
        }

        /// <summary>设置播放结束回调。</summary>
        public void OnFinished(Action callback) => _onFinished = callback;
    }
}
