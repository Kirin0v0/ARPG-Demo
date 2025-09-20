using System;
using Framework.Core.Singleton;
using Sirenix.OdinInspector;

namespace Common
{
    public class SingletonSerializedScriptableObject<T> : SerializedScriptableObject, ISingleton
        where T : SingletonSerializedScriptableObject<T>
    {
        private static T _instance;

        public static T Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = SingletonCreator.CreateScriptableObjectSingleton<T>();
                }

                return _instance;
            }
        }

        public void OnSingletonInit()
        {
            if (_instance != null)
            {
                throw new Exception("Can't invoke OnSingletonInit() when Instance is not null");
            }
        }

        public void Dispose()
        {
            _instance = null;
        }
    }
}