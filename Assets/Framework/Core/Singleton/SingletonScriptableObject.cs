using System;
using UnityEngine;

namespace Framework.Core.Singleton
{
    public class SingletonScriptableObject<T> : ScriptableObject, ISingleton where T : SingletonScriptableObject<T>
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