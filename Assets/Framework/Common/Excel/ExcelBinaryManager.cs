using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Framework.Common.Debug;

namespace Framework.Common.Excel
{
    /// <summary>
    /// 使用Excel2BinaryWindow工具生成的二进制文件的管理类，支持加载文件功能
    /// </summary>
    public class ExcelBinaryManager
    {
        private readonly Dictionary<string, object> _containerDictionary = new();

        /// <summary>
        /// 内部使用反射实现加载数据到内存中，性能较差，建议在进入游戏前提前加载好
        /// </summary>
        /// <param name="binaryPath">二进制文件路径</param>
        /// <param name="ignoreDataExists">是否忽略已有数据，是则在已有数据存在的情况下重复加载，否则不执行加载</param>
        /// <typeparam name="TKey">数据容器类</typeparam>
        /// <typeparam name="TValue">数据类</typeparam>
        public void LoadContainer<TKey, TValue>(string binaryPath, bool ignoreDataExists = false)
        {
            Type containerType = typeof(TKey);

            // 如果已存在数据且不忽略则不进行加载，因为加载比较耗性能
            if (_containerDictionary.ContainsKey(containerType.Name) && !ignoreDataExists)
            {
                return;
            }

            // 反射创建容器类并设置字典
            object containerInstance = Activator.CreateInstance(containerType);
            var dataDictionary = (IDictionary)containerType.GetField("Data").GetValue(containerInstance);

            // 读取二进制文件，二进制格式请见@Excel2BinaryWindow类
            using var fileStream = File.Open(binaryPath, FileMode.Open, FileAccess.Read);
            // DebugUtil.Log($"加载二进制文件: {binaryPath}");

            // 读取主键字段
            var primaryKeyFieldName = GetStringFromFileStream(fileStream);

            // 如果未到末尾则一直按照（每列字段名字符串+每列字段类型字符串+每列行数+每列每行值字符串）的顺序读取
            var dataList = new List<TValue>();
            while (fileStream.Position < fileStream.Length)
            {
                // DebugUtil.Log($"字符位置: {fileStream.Position}, 长度: {fileStream.Length}");
                var fieldName = GetStringFromFileStream(fileStream);
                var dataType = GetStringFromFileStream(fileStream);
                var columnNumber = GetIntFromFileStream(fileStream);
                // DebugUtil.Log($"字段名: {fieldName}");
                // DebugUtil.LogGreen($"数据类型: {dataType}");

                for (var i = 0; i < columnNumber; i++)
                {
                    object value;
                    switch (dataType)
                    {
                        case "int":
                            value = GetIntFromFileStream(fileStream);
                            break;
                        case "float":
                            value = GetFloatFromFileStream(fileStream);
                            break;
                        case "long":
                            value = GetLongFromFileStream(fileStream);
                            break;
                        case "bool":
                            value = GetBoolFromFileStream(fileStream);
                            break;
                        case "string":
                            value = GetStringFromFileStream(fileStream);
                            break;
                        default:
                            continue;
                    }

                    // 创建数据类并添加到列表
                    var classType = typeof(TValue);
                    if (dataList.Count <= i)
                    {
                        dataList.Add((TValue)Activator.CreateInstance(classType));
                    }

                    var dataInstance = dataList[i];
                    // 设置字段值
                    classType.GetField(fieldName).SetValue(dataInstance, value);

                    // 如果是主键则关联数据
                    if (String.Equals(primaryKeyFieldName, fieldName))
                    {
                        dataDictionary.Add(value, dataInstance);
                    }
                }
            }

            if (_containerDictionary.ContainsKey(containerType.Name))
            {
                _containerDictionary[containerType.Name] = containerInstance;
            }
            else
            {
                _containerDictionary.Add(containerType.Name, containerInstance);
            }

            fileStream.Close();

            static string GetStringFromFileStream(FileStream fileStream)
            {
                var length = GetIntFromFileStream(fileStream);
                var bytes = new byte[length];
                fileStream.Read(bytes, 0, bytes.Length);
                return Encoding.UTF8.GetString(bytes);
            }

            static int GetIntFromFileStream(FileStream fileStream)
            {
                var bytes = new byte[4];
                fileStream.Read(bytes, 0, bytes.Length);
                return BitConverter.ToInt32(bytes);
            }

            static float GetFloatFromFileStream(FileStream fileStream)
            {
                var bytes = new byte[4];
                fileStream.Read(bytes, 0, bytes.Length);
                return BitConverter.ToSingle(bytes);
            }

            static long GetLongFromFileStream(FileStream fileStream)
            {
                var bytes = new byte[8];
                fileStream.Read(bytes, 0, bytes.Length);
                return BitConverter.ToInt64(bytes);
            }

            static bool GetBoolFromFileStream(FileStream fileStream)
            {
                var bytes = new byte[1];
                fileStream.Read(bytes, 0, bytes.Length);
                return BitConverter.ToBoolean(bytes);
            }
        }

        public T GetContainer<T>()
        {
            var type = typeof(T);
            if (_containerDictionary.TryGetValue(type.Name, out var value))
            {
                return (T)value;
            }

            return default(T);
        }
    }
}