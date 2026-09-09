using System.Collections.Generic;

namespace StreetFighter.Core
{
    /// <summary>
    /// 原版 config.json 的 keyMap 里存的是 JS keyCode（如 <c>97</c> 代表小键盘 1），
    /// 这里把它们翻译成 Input System 的键盘控件路径（如 <c>&lt;Keyboard&gt;/numpad1</c>），
    /// 这样每个玩家的按键绑定仍然只由 config.json 决定，不需要在 C# 里再写一份。
    /// </summary>
    public static class KeyboardPaths
    {
        private const string Prefix = "<Keyboard>/";

        /// <summary>字母键的起始 keyCode（A）。</summary>
        private const int FirstLetter = 'A';

        /// <summary>字母键的结束 keyCode（Z）。</summary>
        private const int LastLetter = 'Z';

        /// <summary>大小写 keyCode 的差值。</summary>
        private const int LetterCaseOffset = 'a' - 'A';

        /// <summary>数字键的起始 keyCode（0）。</summary>
        private const int FirstDigit = '0';

        /// <summary>数字键的结束 keyCode（9）。</summary>
        private const int LastDigit = '9';

        private static readonly Dictionary<int, string> NamedKeys = new Dictionary<int, string>
        {
            { 8, "backspace" },
            { 9, "tab" },
            { 13, "enter" },
            { 16, "leftShift" },
            { 17, "leftCtrl" },
            { 18, "leftAlt" },
            { 27, "escape" },
            { 32, "space" },
            { 37, "leftArrow" },
            { 38, "upArrow" },
            { 39, "rightArrow" },
            { 40, "downArrow" },

            { 96, "numpad0" },
            { 97, "numpad1" },
            { 98, "numpad2" },
            { 99, "numpad3" },
            { 100, "numpad4" },
            { 101, "numpad5" },
            { 102, "numpad6" },
            { 103, "numpad7" },
            { 104, "numpad8" },
            { 105, "numpad9" },
            { 106, "numpadMultiply" },
            { 107, "numpadPlus" },
            { 109, "numpadMinus" },
            { 110, "numpadPeriod" },
            { 111, "numpadDivide" },

            { 112, "f1" },
            { 113, "f2" },
            { 114, "f3" },
            { 115, "f4" },
            { 116, "f5" },
            { 117, "f6" },
            { 118, "f7" },
            { 119, "f8" },
            { 120, "f9" },
            { 121, "f10" },
            { 122, "f11" },
            { 123, "f12" },
        };

        /// <summary>把 JS keyCode 转成 Input System 的键盘控件路径，无法识别时返回 null。</summary>
        public static string Get(int keyCode)
        {
            string name;
            if (NamedKeys.TryGetValue(keyCode, out name))
            {
                return Prefix + name;
            }

            if (keyCode >= FirstLetter && keyCode <= LastLetter)
            {
                return Prefix + (char)(keyCode + LetterCaseOffset);
            }

            if (keyCode >= FirstDigit && keyCode <= LastDigit)
            {
                return Prefix + (char)keyCode;
            }

            return null;
        }
    }
}
