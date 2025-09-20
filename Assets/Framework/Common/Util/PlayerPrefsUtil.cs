using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using Unity.VisualScripting;
using UnityEngine;
using KeyValuePair = System.Collections.Generic.KeyValuePair;
using Object = System.Object;

namespace Framework.Common.Util
{
    public static class PlayerPrefsUtil
    {
        /**
         * 存储数据，这里主要处理自定义类型的字段储存，非自定义类型则在SetValue中处理储存逻辑
         */
        public static void SaveData(string keyName, object data)
        {
            #region 判断是否为自定义类型，否则执行SaveValue方法

            Type type = data.GetType();
            if (!TypeUtil.IsCustomType(type))
            {
                SaveValue(keyName, data);
                return;
            }

            #endregion

            #region 遍历自定义类型字段，这里规定字段keyName为父类型keyName_父类型数据类型_字段名

            FieldInfo[] fields = type.GetFields();
            foreach (FieldInfo field in fields)
            {
                SaveData(keyName + "_" + type.Name + "_" + field.Name, field.GetValue(data));
            }

            #endregion

            PlayerPrefs.Save();
        }

        /**
         * 储存值数据，只支持部分基本类型以及集合类型
         */
        private static void SaveValue(string keyName, object value)
        {
            // Debug.Log("SaveValue keyName: " + keyName + ", value: " + value);

            Type type = value.GetType();
            keyName = keyName + "_" + type.Name;
            if (type == typeof(int))
            {
                PlayerPrefs.SetInt(keyName, (int)value);
            }
            else if (type == typeof(float))
            {
                PlayerPrefs.SetFloat(keyName, (float)value);
            }
            else if (type == typeof(long))
            {
                PlayerPrefs.SetString(keyName, ((long)value).ToString());
            }
            else if (type == typeof(string))
            {
                PlayerPrefs.SetString(keyName, (string)value);
            }
            else if (type == typeof(bool))
            {
                PlayerPrefs.SetInt(keyName, (bool)value ? 1 : 0);
            }
            else if (typeof(IDictionary).IsAssignableFrom(type))
            {
                IEnumerable enumerable = (IDictionary)value;
                IEnumerator enumerator = enumerable.GetEnumerator();
                int index = 0;

                while (enumerator.MoveNext())
                {
                    // 储存当前子项，这里由于类型未知，因此交由SaveData处理
                    SaveData(keyName + "_key_" + index, (((IDictionaryEnumerator)enumerator).Key));
                    SaveData(keyName + "_value_" + index, (((IDictionaryEnumerator)enumerator).Value));
                    index++;
                }

                // 储存集合数量以便后续读取数量
                PlayerPrefs.SetInt(keyName, index);
            }
            else if (typeof(IList).IsAssignableFrom(type))
            {
                IEnumerable enumerable = (IEnumerable)value;
                IEnumerator enumerator = enumerable.GetEnumerator();
                int index = 0;

                while (enumerator.MoveNext())
                {
                    // 储存当前子项，这里由于类型未知，因此交由SaveData处理
                    SaveData(keyName + "_" + index, enumerator.Current);
                    index++;
                }

                // 储存集合数量以便后续读取数量
                PlayerPrefs.SetInt(keyName, index);
            }
            else
            {
                // 未支持类型抛出异常
                throw new Exception("SaveValue方法未支持的类型，请检查类型或添加处理逻辑");
            }
        }


        public static object LoadData(string keyName, Type type, [CanBeNull] object defaultValue)
        {
            #region 判断是否为自定义类型，否则执行LoadValue方法

            if (!TypeUtil.IsCustomType(type))
            {
                return LoadValue(keyName, type, defaultValue);
            }

            #endregion

            #region 创建自定义类型对象并遍历自定义类型字段设置值，这里规定字段keyName为父类型keyName_父类型数据类型_字段名

            object data = Activator.CreateInstance(type);
            FieldInfo[] fields = type.GetFields();
            foreach (FieldInfo field in fields)
            {
                field.SetValue(data,
                    LoadData(keyName + "_" + type.Name + "_" + field.Name, field.FieldType, defaultValue));
            }

            #endregion

            return data;
        }

        private static object LoadValue(string keyName, Type type, [CanBeNull] object defaultValue)
        {
            // Debug.Log("LoadValue keyName: " + keyName + ", type: " + type);

            keyName = keyName + "_" + type.Name;
            if (type == typeof(int))
            {
                return PlayerPrefs.GetInt(keyName, defaultValue != null ? (int)defaultValue : 0);
            }
            else if (type == typeof(float))
            {
                return PlayerPrefs.GetFloat(keyName, defaultValue != null ? (float)defaultValue : 0f);
            }
            else if (type == typeof(long))
            {
                return long.Parse(PlayerPrefs.GetString(keyName, defaultValue != null ? defaultValue.ToString() : ""));
            }
            else if (type == typeof(string))
            {
                return PlayerPrefs.GetString(keyName, defaultValue != null ? (string)defaultValue : "");
            }
            else if (type == typeof(bool))
            {
                return PlayerPrefs.GetInt(keyName, defaultValue != null ? ((bool)defaultValue ? 1 : 0) : 0) == 1;
            }
            else if (typeof(IDictionary).IsAssignableFrom(type))
            {
                // 读取集合数量
                int enumerableCount = PlayerPrefs.GetInt(keyName, 0);
                IDictionary dictionary = Activator.CreateInstance(type) as IDictionary;
                Type[] genericArguments = type.GetGenericArguments();
                for (int index = 0; index < enumerableCount; index++)
                {
                    // 读取当前子项，这里由于类型未知，因此交由LoadData处理
                    if (dictionary != null)
                    {
                        dictionary.Add(
                            LoadData(keyName + "_key_" + index, genericArguments[0], null),
                            LoadData(keyName + "_value_" + index, genericArguments[1], null)
                        );
                    }
                }

                return dictionary;
            }
            else if (typeof(IList).IsAssignableFrom(type))
            {
                // 读取集合数量
                int enumerableCount = PlayerPrefs.GetInt(keyName, 0);
                IList list = Activator.CreateInstance(type) as IList;
                Type[] genericArguments = type.GetGenericArguments();

                for (int index = 0; index < enumerableCount; index++)
                {
                    // 读取当前子项，这里由于类型未知，因此交由LoadData处理
                    if (list != null)
                    {
                        list.Add(LoadData(keyName + "_" + index, genericArguments[0], null));
                    }
                }

                return list;
            }
            else
            {
                // 未支持类型抛出异常
                throw new Exception("LoadValue方法未支持的类型，请检查类型或添加处理逻辑");
            }
        }
    }
}