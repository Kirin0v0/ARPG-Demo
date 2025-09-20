using System;
using System.Collections;
using System.Collections.Generic;
using Framework.Common.Debug;
using Framework.Core.Event;
using Framework.Core.Singleton;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace Framework.Common.Function
{
    public class SceneEventParameter
    {
        public readonly string SceneName;

        public SceneEventParameter(string sceneName)
        {
            SceneName = sceneName;
        }
    }

    public class SceneLoadingEventParameter : SceneEventParameter
    {
        public readonly float Progress;

        public SceneLoadingEventParameter(string sceneName, float progress) : base(sceneName)
        {
            Progress = progress;
        }
    }

    public class SceneUnloadingEventParameter : SceneEventParameter
    {
        public readonly float Progress;

        public SceneUnloadingEventParameter(string sceneName, float progress) : base(sceneName)
        {
            Progress = progress;
        }
    }

    /// <summary>
    /// 场景管理器，提供切换场景的能力
    /// </summary>
    public class SceneManager : MonoGlobalSingleton<SceneManager>
    {
        // 场景事件
        public UnityAction<SceneEventParameter> OnSceneStartLoad;
        public UnityAction<SceneLoadingEventParameter> OnSceneLoading;
        public UnityAction<SceneEventParameter> OnSceneCompleteLoad;
        public UnityAction<SceneEventParameter> OnSceneStartUnload;
        public UnityAction<SceneUnloadingEventParameter> OnSceneUnloading;
        public UnityAction<SceneEventParameter> OnSceneCompleteUnload;

        /// <summary>
        /// 同步加载场景
        /// </summary>
        /// <param name="name">场景名称</param>
        /// <param name="mode">加载模式</param>
        /// <param name="callBack">加载回调</param>
        public void LoadScene(string name, LoadSceneMode mode, UnityAction callBack = null)
        {
            OnSceneStartLoad?.Invoke(new SceneEventParameter(name));
            // 同步切换场景
            UnityEngine.SceneManagement.SceneManager.LoadScene(name, mode);
            OnSceneCompleteLoad?.Invoke(new SceneEventParameter(name));
            callBack?.Invoke();
        }

        /// <summary>
        /// 异步加载场景
        /// </summary>
        /// <param name="name">场景名称</param>
        /// <param name="mode">加载模式</param>
        /// <param name="callBack">加载回调</param>
        /// <param name="activeSceneUntilLoadComplete">是否直到场景加载完成才激活场景</param>
        /// <param name="minLoadTime">最小加载时间</param>
        public void LoadSceneAsync(
            string name,
            LoadSceneMode mode,
            UnityAction callBack = null,
            bool activeSceneUntilLoadComplete = true,
            float minLoadTime = 0f
        )
        {
            StartCoroutine(LoadSceneAsyncInternal(name, mode, callBack, activeSceneUntilLoadComplete, minLoadTime));
        }

        private IEnumerator LoadSceneAsyncInternal(
            string name,
            LoadSceneMode mode,
            UnityAction callBack = null,
            bool activeSceneUntilLoadComplete = true,
            float minLoadTime = 0f
        )
        {
            var loadTime = 0f;
            OnSceneStartLoad?.Invoke(new SceneEventParameter(name));
            // 异步加载场景
            var asyncOperation = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(name, mode);
            // 如果存在加载句柄，就添加加载进度
            if (asyncOperation != null)
            {
                if (activeSceneUntilLoadComplete)
                {
                    asyncOperation.allowSceneActivation = false;
                }

                // 每帧检测是否加载结束
                while (!asyncOperation.isDone)
                {
                    if (activeSceneUntilLoadComplete)
                    {
                        asyncOperation.allowSceneActivation =
                            asyncOperation.progress >= 0.9f && loadTime >= minLoadTime;
                    }

                    OnSceneLoading?.Invoke(new SceneLoadingEventParameter(name, asyncOperation.progress));
                    yield return 0;
                    loadTime += Time.unscaledDeltaTime;
                }

                while (loadTime < minLoadTime)
                {
                    yield return 0;
                    loadTime += Time.unscaledDeltaTime;
                }

                OnSceneLoading?.Invoke(new SceneLoadingEventParameter(name, 1f));
                asyncOperation.allowSceneActivation = true;
            }

            OnSceneCompleteLoad?.Invoke(new SceneEventParameter(name));
            callBack?.Invoke();
        }

        /// <summary>
        /// 异步卸载场景
        /// </summary>
        /// <param name="name">场景名称</param>
        /// <param name="callBack">卸载回调</param>
        public void UnloadSceneAsync(string name, UnityAction callBack = null)
        {
            StartCoroutine(UnloadSceneAsyncInternal(name, callBack));
        }

        private IEnumerator UnloadSceneAsyncInternal(string name, UnityAction callBack = null)
        {
            OnSceneStartUnload?.Invoke(new SceneEventParameter(name));
            // 异步卸载场景
            var asyncOperation = UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(name);
            if (asyncOperation != null)
            {
                // 每帧检测是否加载结束
                while (!asyncOperation.isDone)
                {
                    OnSceneUnloading?.Invoke(new SceneUnloadingEventParameter(name, asyncOperation.progress));
                    yield return 0;
                }

                OnSceneUnloading?.Invoke(new SceneUnloadingEventParameter(name, 1f));
            }

            OnSceneCompleteUnload?.Invoke(new SceneEventParameter(name));
            callBack?.Invoke();
        }
    }
}