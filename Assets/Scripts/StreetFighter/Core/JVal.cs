using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace StreetFighter
{
    /// <summary>
    /// 极简 JSON 值 + 解析器。Unity 自带的 JsonUtility 不支持字典 / 混合类型数组，
    /// 而原版 config.js 大量使用这两种结构，因此这里自带一个轻量解析。
    /// </summary>
    public sealed class JVal
    {
        public enum Kind { Null, Bool, Number, String, Array, Object }

        public Kind Type = Kind.Null;
        public bool Bool;
        public double Number;
        public string Str;
        public List<JVal> Items;
        public List<string> Keys;
        public List<JVal> Values;

        public static readonly JVal NIL = new JVal();

        public bool IsNull => Type == Kind.Null;

        public int Count
        {
            get
            {
                if (Items != null) return Items.Count;
                if (Keys != null) return Keys.Count;
                return 0;
            }
        }

        public JVal Get(string key)
        {
            if (Type != Kind.Object || Keys == null) return NIL;
            for (int i = 0; i < Keys.Count; i++)
                if (Keys[i] == key) return Values[i];
            return NIL;
        }

        public bool Has(string key)
        {
            if (Type != Kind.Object || Keys == null) return false;
            return Keys.Contains(key);
        }

        public JVal Get(int index)
        {
            if (Type != Kind.Array || Items == null) return NIL;
            if (index < 0 || index >= Items.Count) return NIL;
            return Items[index];
        }

        public float F
        {
            get
            {
                if (Type == Kind.Number) return (float)Number;
                if (Type == Kind.String)
                {
                    float v;
                    if (float.TryParse(Str, NumberStyles.Float, CultureInfo.InvariantCulture, out v)) return v;
                }
                return 0f;
            }
        }

        public int I => (int)F;

        public string S => Type == Kind.String ? Str : null;

        public bool AsBool => Type == Kind.Bool && Bool;

        public override string ToString()
        {
            switch (Type)
            {
                case Kind.Number: return Number.ToString(CultureInfo.InvariantCulture);
                case Kind.String: return Str;
                case Kind.Bool: return Bool.ToString();
                case Kind.Array: return "[" + Items.Count + "]";
                case Kind.Object: return "{" + Keys.Count + "}";
                default: return "null";
            }
        }
    }

    public class Json
    {
        private string _s;
        private int _i;

        public static JVal Parse(string text)
        {
            var p = new Json { _s = text, _i = 0 };
            p.Skip();
            return p.Value();
        }

        private void Skip()
        {
            while (_i < _s.Length && char.IsWhiteSpace(_s[_i])) _i++;
        }

        private char Peek() => _i < _s.Length ? _s[_i] : '\0';

        private JVal Value()
        {
            Skip();
            char c = Peek();
            switch (c)
            {
                case '{': return Object();
                case '[': return Array();
                case '"': return new JVal { Type = JVal.Kind.String, Str = Str() };
                case 't': Expect("true"); return new JVal { Type = JVal.Kind.Bool, Bool = true };
                case 'f': Expect("false"); return new JVal { Type = JVal.Kind.Bool, Bool = false };
                case 'n': Expect("null"); return JVal.NIL;
                default: return Number();
            }
        }

        private void Expect(string word)
        {
            if (_i + word.Length > _s.Length) throw new Exception("json eof: " + word);
            _i += word.Length;
        }

        private JVal Object()
        {
            var v = new JVal { Type = JVal.Kind.Object, Keys = new List<string>(), Values = new List<JVal>() };
            _i++; // {
            Skip();
            if (Peek() == '}') { _i++; return v; }
            while (true)
            {
                Skip();
                string key = Str();
                Skip();
                if (Peek() == ':') _i++;
                v.Keys.Add(key);
                v.Values.Add(Value());
                Skip();
                char c = Peek();
                if (c == ',') { _i++; continue; }
                if (c == '}') { _i++; break; }
                throw new Exception("json object: " + c + " at " + _i);
            }
            return v;
        }

        private JVal Array()
        {
            var v = new JVal { Type = JVal.Kind.Array, Items = new List<JVal>() };
            _i++; // [
            Skip();
            if (Peek() == ']') { _i++; return v; }
            while (true)
            {
                v.Items.Add(Value());
                Skip();
                char c = Peek();
                if (c == ',') { _i++; continue; }
                if (c == ']') { _i++; break; }
                throw new Exception("json array: " + c + " at " + _i);
            }
            return v;
        }

        private string Str()
        {
            if (Peek() != '"') throw new Exception("json string at " + _i);
            _i++;
            var sb = new StringBuilder();
            while (_i < _s.Length)
            {
                char c = _s[_i++];
                if (c == '"') break;
                if (c == '\\')
                {
                    char e = _i < _s.Length ? _s[_i++] : '\0';
                    switch (e)
                    {
                        case 'n': sb.Append('\n'); break;
                        case 't': sb.Append('\t'); break;
                        case 'r': sb.Append('\r'); break;
                        case 'b': sb.Append('\b'); break;
                        case 'f': sb.Append('\f'); break;
                        case 'u':
                            sb.Append((char)Convert.ToInt32(_s.Substring(_i, 4), 16));
                            _i += 4;
                            break;
                        default: sb.Append(e); break;
                    }
                }
                else sb.Append(c);
            }
            return sb.ToString();
        }

        private JVal Number()
        {
            int start = _i;
            while (_i < _s.Length && "+-0123456789.eE".IndexOf(_s[_i]) >= 0) _i++;
            string t = _s.Substring(start, _i - start);
            double d;
            double.TryParse(t, NumberStyles.Float, CultureInfo.InvariantCulture, out d);
            return new JVal { Type = JVal.Kind.Number, Number = d };
        }
    }
}
