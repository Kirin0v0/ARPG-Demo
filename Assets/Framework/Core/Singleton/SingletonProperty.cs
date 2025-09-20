using System;
using UnityEngine;

namespace Framework.Core.Singleton
{
    /// <summary>
    /// 针对普通C#类的单例委托属性类，适用于已有父类的C#类，不影响其继承链
    /// </summary>
    /// <typeparam name="T">实际单例类型</typeparam>
    public static class SingletonProperty<T> where T : class, ISingleton
    {
        private static T _instance;

        private static readonly object LockObject = new();

        public static T GetInstance()
        {
            if (_instance == null)
            {
                lock (LockObject)
                {
                    if (_instance == null)
                    {
                        _instance = SingletonCreator.CreateSingleton<T>();
                    }
                }
            }

            return _instance;
        }

        public static void InitSingleton(System.Action initAction)
        {
            if (_instance != null)
            {
                throw new Exception("Can't invoke OnSingletonInit() when Instance is not null");
            }

            initAction.Invoke();
        }

        public static void Dispose()
        {
            _instance = null;
        }
    }
}