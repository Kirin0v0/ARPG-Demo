using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Sirenix.Utilities;
using UnityEngine;

namespace Buff.Data.Extension
{
    public static class BuffInfoFormatter
    {
        // 预定义占位符，将占位符名称映射到对应的属性获取方法
        private static readonly
            Dictionary<string, (string placeHolderMeaning, Func<BuffInfo, string> placeHolderOutput)>
            PlaceHolderDefinitions = new(StringComparer.OrdinalIgnoreCase)
            {
                { "name", ("Buff名称", buff => buff.name) },
                { "maxStack", ("Buff最大层数", buff => buff.maxStack.ToString()) },
                {
                    "tickTime",
                    ("Buff工作间隔",
                        buff => Mathf.Approximately(buff.tickTime, Mathf.Round(buff.tickTime))
                            ? ((int)Mathf.Round(buff.tickTime)).ToString()
                            : buff.tickTime.ToString("F1")
                    )
                },
                // 后续可根据需要添加其他属性
            };

        public static List<string> GetPlaceHolderDefinitions()
        {
            var definitions = new List<string>();
            PlaceHolderDefinitions.ForEach(pair =>
            {
                var stringBuilder = new StringBuilder();
                stringBuilder.Append("{").Append(pair.Key).Append("}: ")
                    .Append(pair.Value.placeHolderMeaning);
                definitions.Add(stringBuilder.ToString());
            });
            return definitions;
        }

        public static string Format(string format, BuffInfo buffInfo)
        {
            // 使用正则表达式替换所有{key}为实际值
            return Regex.Replace(format, @"\{(\w+)\}", match =>
            {
                string key = match.Groups[1].Value;
                if (PlaceHolderDefinitions.TryGetValue(key, out var value))
                {
                    return value.placeHolderOutput(buffInfo);
                }

                // 保留原始占位符
                return match.Value;
            });
        }
    }
}