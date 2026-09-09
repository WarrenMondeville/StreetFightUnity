using System;
using UnityEngine;

namespace StreetFighter.Core
{
    /// <summary>
    /// 配置分片加载器。原版 config.js 导出的配置按「功能 + 角色」拆成
    /// <c>Assets/Resources/Config/</c> 下的多个 json（每个分片都是同一棵配置树的一个分支），
    /// 运行时全部读出后深度合并成单一根节点，逻辑上等价于原来的单个 config.json。
    /// </summary>
    public static class ConfigLoader
    {
        /// <summary>配置分片所在目录（相对 Resources）。</summary>
        public const string ConfigFolder = "Config";

        /// <summary>读取目录下全部分片并合并成配置根节点。</summary>
        public static JVal Load()
        {
            var assets = Resources.LoadAll<TextAsset>(ConfigFolder);
            Array.Sort(assets, (a, b) => string.CompareOrdinal(a.name, b.name));

            var root = JVal.CreateObject();
            var names = new string[assets.Length];
            for (int i = 0; i < assets.Length; i++)
            {
                names[i] = assets[i].name;
                Merge(root, JsonParser.Parse(assets[i].text));
            }

            if (assets.Length == 0)
            {
                Debug.LogWarning($"[SF] Resources/{ConfigFolder} 下没有找到任何配置分片");
            }
            else
            {
                Debug.Log($"[SF] 载入配置分片 {assets.Length} 个: {string.Join(", ", names)}");
            }

            return root;
        }

        /// <summary>把 source 的字段并入 target：两端都是对象时递归合并，否则覆盖。</summary>
        internal static void Merge(JVal target, JVal source)
        {
            if (source.Kind != JVal.ValueKind.Object)
            {
                return;
            }

            for (int i = 0; i < source.Keys.Count; i++)
            {
                string key = source.Keys[i];
                JVal value = source.Values[i];
                int index = target.Keys.IndexOf(key);

                if (index < 0)
                {
                    target.Keys.Add(key);
                    target.Values.Add(value);
                    continue;
                }

                JVal existing = target.Values[index];
                if (existing.Kind == JVal.ValueKind.Object && value.Kind == JVal.ValueKind.Object)
                {
                    Merge(existing, value);
                }
                else
                {
                    target.Values[index] = value;
                }
            }
        }
    }
}
