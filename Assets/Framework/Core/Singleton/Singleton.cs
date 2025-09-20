using System;
using Unity.VisualScripting;

namespace Framework.Core.Singleton
{
    /// <summary>
    /// 针对普通C#类的全局单例父类
    /// </summary>
    /// <typeparam name="T">实际单例类型</typeparam>
    public abstract class Singleton<T> : ISingleton where T : Singleton<T>
    {
        private static T _instance;

        private static readonly object LockObject = new();

        public static T Instance
        {
            get
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
        }

        public virtual void OnSingletonInit()
        {
            if (_instance != null)
            {
                throw new Exception("Can't invoke OnSingletonInit() when Instance is not null");
            }
        }

        public virtual void Dispose()
        {
            _instance = null;
        }
    }
}