using System;
using Framework.Common.Debug;
using UnityEngine;

namespace Framework.Core.Singleton
{
    /// <summary>
    /// 针对MonoBehaviour的全局全场景单例父类，调用该类的Mono类注意在Destroy阶段判空处理，避免ApplicationQuit函数返回空对象导致的空异常
    /// </summary>
    /// <typeparam name="T">实际单例类型</typeparam>
    public abstract class MonoGlobalSingleton<T> : MonoBehaviour, ISingleton where T : MonoGlobalSingleton<T>
    {
        private static T _instance;

        private static bool _applicationQuit;

        public static T Instance
        {
            get
            {
                if (_instance == null && !_applicationQuit)
                {
                    _instance = SingletonCreator.CreateMonoSingleton<T>(true);
                }

                return _instance;
            }
        }

        public static void Reset()
        {
        }

        /// <summary>
        /// 在场景加载前会重置标识符，该函数需要手动调用
        /// </summary>
        public static void OnApplicationEnter()
        {
            _applicationQuit = false;
        }

        /// <summary>
        /// 注意，MonoBehaviour单例在退出播放后再次被调用可能会被创建作为无法获取的单例存在于编辑场景下，因此我们需要确保单例在退出播放后不能被再次创建
        /// </summary>
        protected virtual void OnApplicationQuit()
        {
            _applicationQuit = true;
        }

        protected virtual void OnDestroy()
        {
            Dispose();
        }

        public virtual void OnSingletonInit()
        {
            if (_instance != null)
            {
                throw new Exception("Can't invoke OnSingletonInit() when instance is not null");
            }
        }

        public virtual void Dispose()
        {
            if (Application.isPlaying)
            {
                Destroy(gameObject);
            }
            else
            {
                DestroyImmediate(gameObject);
            }

            _instance = null;
        }
    }
}