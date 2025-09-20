using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.VisualScripting;
using UnityEngine;

namespace Framework.Common.Util
{
    public static class TypeUtil
    {
        /// <summary>
        /// 判断是否为自定义类型且不为集合
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static bool IsCustomType(Type type)
        {
            return (type != typeof(object) && Type.GetTypeCode(type) == TypeCode.Object) && !IsEnumerableType(type);
        }

        /// <summary>
        /// 判断是否为集合
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static bool IsEnumerableType(Type type)
        {
            return type.GetInterface("IEnumerable") != null;
        }

        /// <summary>
        /// 判断是否是泛型类的子类
        /// </summary>
        /// <param name="child"></param>
        /// <param name="parentGenericType"></param>
        /// <returns></returns>
        public static bool IsSubclassOfGenericType(Type child, Type parentGenericType)
        {
            if (child == parentGenericType)
            {
                return false;
            }

            while (child != null && child != typeof(object))
            {
                var cur = child.IsGenericType ? child.GetGenericTypeDefinition() : child;
                if (parentGenericType == cur)
                {
                    return true;
                }

                child = child.BaseType;
            }

            return false;
        }
    }
}