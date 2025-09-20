using System;
using System.Collections;
using System.Collections.Generic;
using Framework.Core.Singleton;
using UnityEngine;
using UnityEngine.Events;
using Object = UnityEngine.Object;

namespace Framework.Common.Resource
{
    /// <summary>
    /// Resources资源加载管理器，建议在Load方法调用时添加Unload方法平衡引用计数。如果嫌麻烦可以直接使用ForceClear方法无视引用计数卸载无用资源。
    /// </summary>
    public class ResourcesManager : MonoBehaviour
    {
        /// <summary>
        /// 资源加载基类，内置引用计数，主要用于里式替换原则
        /// </summary>
        private abstract class BaseResourcesLoad
        {
            // 引用计数
            private int _refCount;

            public void AddRefCount()
            {
                ++_refCount;
            }

            public void SubtractRefCount()
            {
                --_refCount;
                if (_refCount < 0)
                    UnityEngine.Debug.LogError("当前引用计数小于0，请检查使用和卸载是否配对执行");
            }

            public bool CanUnload() => _refCount == 0;
        }

        /// <summary>
        /// 资源加载对象，主要用于存储资源信息/异步加载委托信息/协程信息
        /// </summary>
        /// <typeparam name="T">资源类型</typeparam>
        private class ResourcesLoad<T> : BaseResourcesLoad
        {
            // 是否正在加载资源
            public bool Loading;

            // 加载的资源
            public T Asset;

            // 用于异步加载结束后传递资源到外部的委托
            public UnityAction<T> LoadAsyncAction;

            // 用于存储异步加载时开启的协同程序
            public Coroutine RunningCoroutine;
        }

        // 存储资源名称_资源类型<-->资源加载类的字典
        private readonly Dictionary<string, BaseResourcesLoad> _resourcesLoads = new();

        /// <summary>
        /// 同步加载Resources文件夹下的资源
        /// </summary>
        /// <param name="path">资源路径，是Resources/后的路径（不包含Resources/）</param>
        /// <typeparam name="T">资源文件类型</typeparam>
        /// <returns>资源对象</returns>
        public T Load<T>(string path) where T : UnityEngine.Object
        {
            var resName = GetResourceName(path, typeof(T));
            ResourcesLoad<T> load;

            // 字典中不存在加载资源时
            if (!_resourcesLoads.ContainsKey(resName))
            {
                // 直接同步加载并且记录资源信息到字典中，方便下次直接取出来用
                T res = Resources.Load<T>(path);
                load = new ResourcesLoad<T>();
                load.Loading = false;
                load.Asset = res;
                load.AddRefCount();
                _resourcesLoads.Add(resName, load);
                return res;
            }

            // 字典中存在加载资源时
            load = _resourcesLoads[resName] as ResourcesLoad<T>;
            load!.AddRefCount();

            // 资源正在加载中时
            if (load.Loading)
            {
                // 停止异步加载，设置同步结果并执行异步回调
                StopCoroutine(load.RunningCoroutine);
                T res = Resources.Load<T>(path);
                load.Loading = false;
                load.Asset = res;
                load.LoadAsyncAction?.Invoke(res);
                load.LoadAsyncAction = null;
                load.RunningCoroutine = null;
                return res;
            }

            // 资源已加载完毕则直接使用
            return load.Asset;
        }

        /// <summary>
        /// 异步加载Resources文件夹下的资源
        /// </summary>
        /// <param name="path">资源路径，是Resources/后的路径（不包含Resources/）</param>
        /// <param name="callback">获取资源后的回调</param>
        /// <typeparam name="T">资源文件类型</typeparam>
        public void LoadAsync<T>(string path, UnityAction<T> callback) where T : UnityEngine.Object
        {
            var resName = GetResourceName(path, typeof(T));
            ResourcesLoad<T> load;

            // 字典中不存在加载资源时
            if (!_resourcesLoads.TryGetValue(resName, out var resourcesLoad))
            {
                // 直接异步加载并且记录加载信息到字典中，方便下次直接取出来用
                load = new ResourcesLoad<T>();
                load.Loading = true;
                load.Asset = null;
                load.AddRefCount();
                load.LoadAsyncAction += callback;
                load.RunningCoroutine = StartCoroutine(LoadAsyncInternal<T>(path));
                _resourcesLoads.Add(resName, load);
                return;
            }

            // 字典中存在加载资源时
            load = resourcesLoad as ResourcesLoad<T>;
            load!.AddRefCount();

            if (load.Loading) // 资源正在加载中时
            {
                // 添加回调到委托中
                load.LoadAsyncAction += callback;
            }
            else // 资源已加载时
            {
                // 直接执行回调
                callback?.Invoke(load.Asset);
            }
        }

        /// <summary>
        /// 将对应字典数据的引用计数-1并删除监听回调，并判断其是否值=0，是则直接调用UnloadAsset卸载资源
        /// </summary>
        /// <param name="path">资源路径</param>
        /// <param name="callback">之前绑定的回调</param>
        /// <typeparam name="T">资源类型</typeparam>
        public void UnloadAsset<T>(string path, UnityAction<T> callback = null) where T : Object
        {
            UnloadAssetInternal(path, callback);
        }

        /// <summary>
        /// 根据引用计数清空引用=0的字典数据，并调用UnloadUnusedAssets卸载无用资源。
        /// </summary>
        /// <param name="callback">卸载回调</param>
        public void UnloadUnusedAssets(UnityAction callback)
        {
            StartCoroutine(UnloadUnusedAssetsInternal(callback));
        }

        /// <summary>
        /// 无视引用计数清空字典，并调用UnloadUnusedAssets卸载无用资源。
        /// </summary>
        /// <param name="callback">卸载回调</param>
        public void ForceClear(UnityAction callback)
        {
            StartCoroutine(ForceClearInternal(callback));
        }

        private IEnumerator LoadAsyncInternal<T>(string path) where T : UnityEngine.Object
        {
            // 异步加载资源
            ResourceRequest resourceRequest = Resources.LoadAsync<T>(path);
            yield return resourceRequest;

            var resName = GetResourceName(path, typeof(T));
            // 存在加载资源时
            if (_resourcesLoads.TryGetValue(resName, out var load))
            {
                // 取出资源信息并且记录加载完成的资源
                ResourcesLoad<T> resourcesLoad = load as ResourcesLoad<T>;
                resourcesLoad!.Loading = false;
                resourcesLoad.Asset = resourceRequest.asset as T;

                // 由于异步加载存在外部调用Unload无法及时删除的场景，所以在异步加载结束后判断引用计数是否允许删除
                if (resourcesLoad.CanUnload()) // 允许卸载
                {
                    UnloadAssetInternal<T>(path, null, false);
                }
                else // 不卸载
                {
                    // 将加载完成的资源传递出去，并清空引用防止内存泄漏
                    resourcesLoad.LoadAsyncAction?.Invoke(resourcesLoad.Asset);
                    resourcesLoad.LoadAsyncAction = null;
                    resourcesLoad.RunningCoroutine = null;
                }
            }
        }

        private void UnloadAssetInternal<T>(string path, UnityAction<T> callback = null, bool allowSubtract = true)
        {
            var resName = GetResourceName(path, typeof(T));

            // 存在加载资源时卸载，否则直接不处理
            if (_resourcesLoads.TryGetValue(resName, out var load))
            {
                ResourcesLoad<T> resourcesLoad = load as ResourcesLoad<T>;
                // 如果允许引用-1则减去引用，这里使用参数判断是防止在异步加载后重复减去引用
                if (allowSubtract)
                {
                    resourcesLoad!.SubtractRefCount();
                }

                if (!resourcesLoad!.Loading && resourcesLoad.CanUnload()) // 如果资源完成加载且无外部引用 
                {
                    // 从字典移除并卸载资源
                    _resourcesLoads.Remove(resName);
                    Resources.UnloadAsset(resourcesLoad.Asset as Object);
                }

                if (resourcesLoad!.Loading) // 资源正在异步加载中
                {
                    // 存在卸载在异步加载前，异步加载后仍然存在引用资源的场景，因此等待到异步加载后判断引用来卸载资源
                    if (callback != null)
                    {
                        resourcesLoad.LoadAsyncAction -= callback;
                    }
                }
            }
        }

        private IEnumerator UnloadUnusedAssetsInternal(UnityAction callback = null)
        {
            // 移除那些引用计数允许卸载的加载资源
            List<string> list = new List<string>();
            foreach (var resName in _resourcesLoads.Keys)
            {
                if (_resourcesLoads[resName].CanUnload())
                {
                    list.Add(resName);
                }
            }

            foreach (var resName in list)
            {
                _resourcesLoads.Remove(resName);
            }

            yield return Resources.UnloadUnusedAssets();

            // 卸载完毕后通知外部
            callback?.Invoke();
        }

        private IEnumerator ForceClearInternal(UnityAction callback)
        {
            _resourcesLoads.Clear();
            yield return Resources.UnloadUnusedAssets();

            // 卸载完毕后通知外部
            callback?.Invoke();
        }

        private static string GetResourceName(string path, Type type) => path + "_" + type.Name;
    }
}