using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

namespace Framework.Common.Util
{
    public static class BinaryUtil
    {
        public static void SaveFileData<T>(string path, T data)
        {
            using var fileStream = File.Open(path, FileMode.OpenOrCreate, FileAccess.Write);
            var binaryFormatter = new BinaryFormatter();
            binaryFormatter.Serialize(fileStream, data);
            fileStream.Flush();
            fileStream.Close();
        }

        public static T LoadFileData<T>(string path)
        {
            using var fileStream = File.Open(path, FileMode.Open, FileAccess.Read);
            var binaryFormatter = new BinaryFormatter();
            var data = (T)binaryFormatter.Deserialize(fileStream);
            fileStream.Close();
            return data;
        }

        public static T LoadBytesData<T>(byte[] bytes)
        {
            using var memoryStream = new MemoryStream(bytes);
            var binaryFormatter = new BinaryFormatter();
            var data = (T)binaryFormatter.Deserialize(memoryStream);
            memoryStream.Close();
            return data;
        }
    }
}