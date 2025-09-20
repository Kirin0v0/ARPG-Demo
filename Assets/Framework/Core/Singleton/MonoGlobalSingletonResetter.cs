using System;
using System.Reflection;
using Framework.Common.Util;

namespace Framework.Core.Singleton
{
    public static class MonoGlobalSingletonResetter
    {
        public static void Reset()
        {
            var baseType = typeof(MonoGlobalSingleton<>);
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var type in assembly.GetTypes())
                {
                    if (TypeUtil.IsSubclassOfGenericType(type, baseType))
                    {
                        var method = type.GetMethod("OnApplicationEnter",
                            BindingFlags.Static | BindingFlags.Public | BindingFlags.FlattenHierarchy);
                        if (method != null)
                        {
                            method.Invoke(null, null);
                        }
                    }
                }
            }
        }
    }
}