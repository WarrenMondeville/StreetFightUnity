using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace StreetFighter.Core
{
    /// <summary>
    /// 与 <see cref="JVal"/> 配套的递归下降 JSON 解析器。
    /// 只覆盖 config.json 用到的语法：对象、数组、字符串（含转义）、数字、true/false/null。
    /// </summary>
    public static class JsonParser
    {
        private const string NumberChars = "+-0123456789.eE";

        private sealed class Reader
        {
            private readonly string _text;
            private int _index;

            public Reader(string text)
            {
                _text = text;
                _index = 0;
            }

            public JVal Parse()
            {
                SkipWhitespace();
                return ReadValue();
            }

            private void SkipWhitespace()
            {
                while (_index < _text.Length && char.IsWhiteSpace(_text[_index]))
                {
                    _index++;
                }
            }

            private char Peek() => _index < _text.Length ? _text[_index] : '\0';

            private JVal ReadValue()
            {
                SkipWhitespace();
                switch (Peek())
                {
                    case '{': return ReadObject();
                    case '[': return ReadArray();
                    case '"': return JVal.CreateString(ReadString());
                    case 't': Expect("true"); return JVal.CreateBool(true);
                    case 'f': Expect("false"); return JVal.CreateBool(false);
                    case 'n': Expect("null"); return JVal.Nil;
                    default: return ReadNumber();
                }
            }

            private void Expect(string word)
            {
                if (_index + word.Length > _text.Length)
                {
                    throw new FormatException($"JSON 解析意外结束，期望: {word}");
                }

                _index += word.Length;
            }

            private JVal ReadObject()
            {
                var value = JVal.CreateObject();
                _index++; // {
                SkipWhitespace();

                if (Peek() == '}')
                {
                    _index++;
                    return value;
                }

                while (true)
                {
                    SkipWhitespace();
                    string key = ReadString();
                    SkipWhitespace();
                    if (Peek() == ':')
                    {
                        _index++;
                    }

                    value.Keys.Add(key);
                    value.Values.Add(ReadValue());

                    SkipWhitespace();
                    char c = Peek();
                    if (c == ',')
                    {
                        _index++;
                        continue;
                    }

                    if (c == '}')
                    {
                        _index++;
                        break;
                    }

                    throw new FormatException($"JSON 对象语法错误: '{c}' @ {_index}");
                }

                return value;
            }

            private JVal ReadArray()
            {
                var value = JVal.CreateArray();
                _index++; // [
                SkipWhitespace();

                if (Peek() == ']')
                {
                    _index++;
                    return value;
                }

                while (true)
                {
                    value.Items.Add(ReadValue());
                    SkipWhitespace();
                    char c = Peek();
                    if (c == ',')
                    {
                        _index++;
                        continue;
                    }

                    if (c == ']')
                    {
                        _index++;
                        break;
                    }

                    throw new FormatException($"JSON 数组语法错误: '{c}' @ {_index}");
                }

                return value;
            }

            private string ReadString()
            {
                if (Peek() != '"')
                {
                    throw new FormatException($"JSON 字符串语法错误 @ {_index}");
                }

                _index++;
                var sb = new StringBuilder();
                while (_index < _text.Length)
                {
                    char c = _text[_index++];
                    if (c == '"')
                    {
                        break;
                    }

                    if (c != '\\')
                    {
                        sb.Append(c);
                        continue;
                    }

                    char escape = _index < _text.Length ? _text[_index++] : '\0';
                    switch (escape)
                    {
                        case 'n': sb.Append('\n'); break;
                        case 't': sb.Append('\t'); break;
                        case 'r': sb.Append('\r'); break;
                        case 'b': sb.Append('\b'); break;
                        case 'f': sb.Append('\f'); break;
                        case 'u':
                            sb.Append((char)Convert.ToInt32(_text.Substring(_index, 4), 16));
                            _index += 4;
                            break;
                        default: sb.Append(escape); break;
                    }
                }

                return sb.ToString();
            }

            private JVal ReadNumber()
            {
                int start = _index;
                while (_index < _text.Length && NumberChars.IndexOf(_text[_index]) >= 0)
                {
                    _index++;
                }

                string token = _text.Substring(start, _index - start);
                double number;
                double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out number);
                return JVal.CreateNumber(number);
            }
        }

        /// <summary>解析 JSON 文本，返回根节点。</summary>
        public static JVal Parse(string text) => new Reader(text).Parse();
    }
}
