using System.Collections.Generic;
using System.Globalization;

namespace StreetFighter.Core
{
    /// <summary>
    /// 极简 JSON 值。Unity 自带的 JsonUtility 不支持字典 / 混合类型数组，
    /// 而原版 config.js 大量使用这两种结构，因此这里自带一个轻量表示 + 解析器（见 <see cref="JsonParser"/>）。
    /// </summary>
    public sealed class JVal
    {
        /// <summary>值的类型。</summary>
        public enum ValueKind
        {
            Null,
            Bool,
            Number,
            String,
            Array,
            Object,
        }

        /// <summary>共享的空值，读取缺失字段时返回它。</summary>
        public static readonly JVal Nil = new JVal();

        /// <summary>值类型。</summary>
        public ValueKind Kind { get; private set; } = ValueKind.Null;

        /// <summary>布尔值（Kind 为 Bool 时有效）。</summary>
        public bool BoolValue { get; private set; }

        /// <summary>数值（Kind 为 Number 时有效）。</summary>
        public double Number { get; private set; }

        /// <summary>字符串（Kind 为 String 时有效）。</summary>
        public string StringValue { get; private set; }

        /// <summary>数组元素（Kind 为 Array 时有效）。</summary>
        public List<JVal> Items { get; private set; }

        /// <summary>对象键（Kind 为 Object 时有效）。</summary>
        public List<string> Keys { get; private set; }

        /// <summary>对象值（Kind 为 Object 时有效，与 <see cref="Keys"/> 一一对应）。</summary>
        public List<JVal> Values { get; private set; }

        /// <summary>是否为 null / 缺失字段。</summary>
        public bool IsNull => Kind == ValueKind.Null;

        /// <summary>数组长度或对象键数量。</summary>
        public int Count
        {
            get
            {
                if (Items != null)
                {
                    return Items.Count;
                }

                return Keys?.Count ?? 0;
            }
        }

        /// <summary>按 key 读对象字段，缺失返回 <see cref="Nil"/>。</summary>
        public JVal Get(string key)
        {
            if (Kind != ValueKind.Object || Keys == null)
            {
                return Nil;
            }

            for (int i = 0; i < Keys.Count; i++)
            {
                if (Keys[i] == key)
                {
                    return Values[i];
                }
            }

            return Nil;
        }

        /// <summary>对象是否包含指定 key。</summary>
        public bool Has(string key)
        {
            if (Kind != ValueKind.Object || Keys == null)
            {
                return false;
            }

            return Keys.Contains(key);
        }

        /// <summary>按下标读数组元素，越界返回 <see cref="Nil"/>。</summary>
        public JVal Get(int index)
        {
            if (Kind != ValueKind.Array || Items == null)
            {
                return Nil;
            }

            if (index < 0 || index >= Items.Count)
            {
                return Nil;
            }

            return Items[index];
        }

        /// <summary>以 float 读取；字符串会尝试解析。</summary>
        public float AsFloat
        {
            get
            {
                if (Kind == ValueKind.Number)
                {
                    return (float)Number;
                }

                if (Kind == ValueKind.String)
                {
                    float value;
                    if (float.TryParse(StringValue, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
                    {
                        return value;
                    }
                }

                return 0f;
            }
        }

        /// <summary>以 int 读取。</summary>
        public int AsInt => (int)AsFloat;

        /// <summary>以 string 读取，非字符串返回 null。</summary>
        public string AsString => Kind == ValueKind.String ? StringValue : null;

        /// <summary>以 bool 读取。</summary>
        public bool AsBool => Kind == ValueKind.Bool && BoolValue;

        /// <summary>原地改写成数字（角色配置里有个别字段需要在运行时微调）。</summary>
        public void SetNumber(double value)
        {
            Kind = ValueKind.Number;
            Number = value;
        }

        public override string ToString()
        {
            switch (Kind)
            {
                case ValueKind.Number: return Number.ToString(CultureInfo.InvariantCulture);
                case ValueKind.String: return StringValue;
                case ValueKind.Bool: return BoolValue.ToString();
                case ValueKind.Array: return $"[{Items.Count}]";
                case ValueKind.Object: return $"{{{Keys.Count}}}";
                default: return "null";
            }
        }

        internal static JVal CreateArray() =>
            new JVal { Kind = ValueKind.Array, Items = new List<JVal>() };

        internal static JVal CreateObject() =>
            new JVal { Kind = ValueKind.Object, Keys = new List<string>(), Values = new List<JVal>() };

        internal static JVal CreateString(string value) =>
            new JVal { Kind = ValueKind.String, StringValue = value };

        internal static JVal CreateBool(bool value) =>
            new JVal { Kind = ValueKind.Bool, BoolValue = value };

        internal static JVal CreateNumber(double value) =>
            new JVal { Kind = ValueKind.Number, Number = value };
    }
}
