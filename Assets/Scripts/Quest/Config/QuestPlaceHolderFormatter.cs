using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Sirenix.Utilities;

namespace Quest.Config
{
    public class QuestPlaceHolderFormatter<T>
    {
        private readonly Dictionary<string, (string placeHolderMeaning, Func<T, string> placeHolderOutput)>
            _placeHolderDefinitions;

        public QuestPlaceHolderFormatter(Dictionary<string, (string placeHolderMeaning, Func<T, string> placeHolderOutput)> placeHolderDefinitions)
        {
            _placeHolderDefinitions = placeHolderDefinitions;
        }

        public List<string> GetDefinitions()
        {
            var definitions = new List<string>();
            _placeHolderDefinitions.ForEach(pair =>
            {
                var stringBuilder = new StringBuilder();
                stringBuilder.Append("{").Append(pair.Key).Append("}: ")
                    .Append(pair.Value.placeHolderMeaning);
                definitions.Add(stringBuilder.ToString());
            });
            return definitions;
        }

        public string Format(string format, T data)
        {
            // 使用正则表达式替换所有{key}为实际值
            return Regex.Replace(format, @"\{(\w+)\}", match =>
            {
                string key = match.Groups[1].Value;
                if (_placeHolderDefinitions.TryGetValue(key, out var value))
                {
                    return value.placeHolderOutput(data);
                }

                // 保留原始占位符
                return match.Value;
            });
        }
    }
}