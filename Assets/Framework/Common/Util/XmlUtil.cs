using System.IO;
using System.Xml.Serialization;

namespace Framework.Common.Util
{
    public static class XmlUtil
    {
        public static void SaveXml<T>(string path, T value)
        {
            using var streamWriter = new StreamWriter(path);
            var xmlSerializer = new XmlSerializer(typeof(T));
            xmlSerializer.Serialize(streamWriter, value);
        }

        public static T LoadXml<T>(string path)
        {
            using var streamReader = new StreamReader(path);
            var xmlSerializer = new XmlSerializer(typeof(T));
            return (T)xmlSerializer.Deserialize(streamReader);
        }
    }
}