using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Framework.Core.Singleton
{
    /// <summary>
    /// 针对MonoBehaviour的单例委托属性类，适用于已有父类的MonoBehaviour，不影响其继承链
    /// </summary>
    /// <typeparam name="T">实际单例类型</typeparam>
    public static class MonoSingletonProperty<T> where T : MonoBehaviour, ISingleton
    {
        private static T _instance;

        public static T GetInstance(bool isGlobalSingleton)
        {
            if (_instance == null)
            {
                _instance = SingletonCreator.CreateMonoSingleton<T>(isGlobalSingleton);
            }

            return _instance;
        }

        public static void InitSingleton(System.Action initAction = null)
        {
            if (_instance != null)
            {
                throw new Exception("Can't invoke OnSingletonInit() when Instance is not null");
            }

            initAction?.Invoke();
        }

        public static void Dispose()
        {
            if (_instance != null)
            {
                Object.Destroy(_instance.gameObject);
            }

            _instance = default(T);
        }
    }
}