using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Framework.Common.LitJson;
using UnityEngine;

namespace Framework.Common.Util
{
    public static class JsonUtil
    {
        public enum Strategy
        {
            JsonUtility,
            LitJson,
        }

        public static T LoadJson<T>(string path, Strategy strategy = Strategy.JsonUtility) where T : new()
        {
            if (!File.Exists(path))
            {
                return new T();
            }

            switch (strategy)
            {
                case Strategy.LitJson:
                    return JsonMapper.ToObject<T>(File.ReadAllText(path));
                case Strategy.JsonUtility:
                default:
                    return JsonUtility.FromJson<T>(File.ReadAllText(path));
            }
        }

        public static void SaveJson(string path, object jsonObject, Strategy strategy = Strategy.JsonUtility)
        {
            switch (strategy)
            {
                case Strategy.LitJson:
                    File.WriteAllText(path, JsonMapper.ToJson(jsonObject));
                    break;
                case Strategy.JsonUtility:
                default:
                    File.WriteAllText(path, JsonUtility.ToJson(jsonObject));
                    break;
            }
        }

        public static string ToJson(object jsonObject, Strategy strategy = Strategy.JsonUtility)
        {
            return strategy switch
            {
                Strategy.LitJson => JsonMapper.ToJson(jsonObject),
                Strategy.JsonUtility => JsonUtility.ToJson(jsonObject),
                _ => JsonMapper.ToJson(jsonObject),
            };
        }

        public static T ToObject<T>(string jsonString, Strategy strategy = Strategy.JsonUtility)
        {
            return strategy switch
            {
                Strategy.LitJson => JsonMapper.ToObject<T>(jsonString),
                Strategy.JsonUtility => JsonUtility.FromJson<T>(jsonString),
                _ => JsonUtility.FromJson<T>(jsonString),
            };
        }
    }
}