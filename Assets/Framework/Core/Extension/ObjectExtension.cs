using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Framework.Core.Extension
{
    public static class ObjectExtension
    {
        public static bool IsDestroyed(this GameObject gameObject)
        {
            if (gameObject == null || gameObject is null || gameObject.Equals(null))
                return true;

            try
            {
                var _ = gameObject.transform;
                return false;
            }
            catch
            {
                return true;
            }
        }

        public static bool IsGameObjectDestroyed(this Object obj)
        {
            try
            {
                switch (obj)
                {
                    case Component component:
                    {
                        return IsDestroyed(component.gameObject);
                    }
                        break;
                    case GameObject gameObject:
                    {
                        return IsDestroyed(gameObject);
                    }
                        break;
                }
            }
            catch (Exception e)
            {
                return true;
            }

            return true;
        }

        public static T DeepClone<T>(this T toClone)
        {
            using (var stream = new MemoryStream())
            {
                var formatter = new BinaryFormatter();
                formatter.Serialize(stream, toClone);
                stream.Seek(0, SeekOrigin.Begin);
                return (T)formatter.Deserialize(stream);
            }
        }
    }
}