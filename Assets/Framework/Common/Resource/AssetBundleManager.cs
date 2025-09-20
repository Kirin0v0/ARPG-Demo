using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using Framework.Core.Singleton;
using UnityEngine;
using UnityEngine.Events;
using Object = UnityEngine.Object;

namespace Framework.Common.Resource
{
    /// <summary>
    /// AB包资源加载管理器，内部管理加载/卸载资源避免重复加载导致API报错
    /// </summary>
    public class AssetBundleManager: MonoBehaviour
    {
        /// <summary>
        /// 资源加载基类，主要用于里式替换原则
        /// </summary>
        private abstract class BaseAssetBundleLoad
        {
        }

        /// <summary>
        /// 资源加载中类，用于异步等待加载完毕
        /// </summary>
        private class AssetBundleLoading : BaseAssetBundleLoad
        {
            public readonly AssetBundleCreateRequest CreateRequest;

            public AssetBundleLoading(AssetBundleCreateRequest createRequest)
            {
                CreateRequest = createRequest;
            }
        }

        /// <summary>
        /// 资源加载完成类，存放资源
        /// </summary>
        private class AssetBundleLoadComplete : BaseAssetBundleLoad
        {
            public readonly AssetBundle AssetBundle;

            public AssetBundleLoadComplete(AssetBundle assetBundle)
            {
                AssetBundle = assetBundle;
            }
        }

        /// <summary>
        /// 资源待卸载类，内置委托，等待卸载后统一执行委托
        /// </summary>
        private class AssetBundleUnload : BaseAssetBundleLoad
        {
            public readonly bool Async;
            public readonly bool UnloadAllLoadedObjects;
            public UnityAction UnloadAction;

            public AssetBundleUnload(bool async, bool unloadAllLoadedObjects, UnityAction unloadAction)
            {
                Async = async;
                UnloadAllLoadedObjects = unloadAllLoadedObjects;
                UnloadAction += unloadAction;
            }
        }

        /// <summary>
        /// 获取AB包加载路径，默认是StreamingAssets
        /// </summary>
        private static string PrefixPath => Application.streamingAssetsPath + "/";

        public string mainAssetBundleName;

        // AB包文件名称<——>AB包加载对象字典
        private readonly Dictionary<string, BaseAssetBundleLoad> _assetBundleLoads = new();

        // (AB包文件名称，协程id)<——>加载协程对象字典
        private readonly Dictionary<(string assetBundleName, uint coroutineId), Coroutine>
            _loadCoroutines = new();

        // 协程序号
        private uint _coroutineId;

        // 主包
        private AssetBundle? _mainAssetBundle;

        // 主包依赖
        private AssetBundleManifest? _manifest;

        public AssetBundleManager()
        {
            if (String.IsNullOrEmpty(mainAssetBundleName))
            {
                throw new NullReferenceException("The name of main asset bundle must be not empty");
            }
        }

        /// <summary>
        /// 同步加载AB包内的资源，注意由于统一管理加载/卸载，存在同帧加载时被卸载的场景，因此有回调值传空的极端情况
        /// </summary>
        /// <param name="assetBundleName">AB包名称</param>
        /// <param name="assetName">资源名称</param>
        /// <param name="callback">加载回调</param>
        /// <typeparam name="T">资源类型</typeparam>
        public void LoadAsset<T>(string assetBundleName, string assetName, UnityAction<T?> callback) where T : Object
        {
            var coroutineId = ++_coroutineId;
            var coroutine = StartCoroutine(
                LoadAssetInternal(
                    assetBundleName,
                    assetName,
                    typeof(T),
                    (obj) => { callback?.Invoke((T)obj); },
                    false,
                    coroutineId
                )
            );
            _loadCoroutines.Add((assetBundleName, coroutineId), coroutine);
        }

        /// <summary>
        /// 同步加载AB包内的资源，注意由于统一管理加载/卸载，存在同帧加载时被卸载的场景，因此有回调值传空的极端情况
        /// </summary>
        /// <param name="assetBundleName">AB包名称</param>
        /// <param name="assetName">资源名称</param>
        /// <param name="type">资源类型</param>
        /// <param name="callback">加载回调</param>
        public void LoadAsset(string assetBundleName, string assetName, System.Type type, UnityAction<Object?> callback)
        {
            var coroutineId = ++_coroutineId;
            var coroutine = StartCoroutine(
                LoadAssetInternal(
                    assetBundleName,
                    assetName,
                    type,
                    callback,
                    false,
                    coroutineId
                )
            );
            _loadCoroutines.Add((assetBundleName, coroutineId), coroutine);
        }

        // ReSharper disable Unity.PerformanceAnalysis
        /// <summary>
        /// 异步加载AB包内的资源，注意由于统一管理加载/卸载，存在同帧加载时被卸载的场景，因此有回调值传空的极端情况
        /// </summary>
        /// <param name="assetBundleName">AB包名称</param>
        /// <param name="assetName">资源名称</param>
        /// <param name="callback">加载回调</param>
        /// <typeparam name="T">资源类型</typeparam>
        public void LoadAssetAsync<T>(string assetBundleName, string assetName, UnityAction<T?> callback)
            where T : Object
        {
            var coroutineId = ++_coroutineId;
            var coroutine = StartCoroutine(
                LoadAssetInternal(
                    assetBundleName,
                    assetName,
                    typeof(T),
                    (obj) => { callback?.Invoke((T?)obj); },
                    true,
                    coroutineId
                )
            );
            _loadCoroutines.Add((assetBundleName, coroutineId), coroutine);
        }

        /// <summary>
        /// 异步加载AB包内的资源，注意由于统一管理加载/卸载，存在同帧加载时被卸载的场景，因此有回调值传空的极端情况
        /// </summary>
        /// <param name="assetBundleName">AB包名称</param>
        /// <param name="assetName">资源名称</param>
        /// <param name="type">资源类型</param>
        /// <param name="callback">加载回调</param>
        public void LoadAssetAsync(string assetBundleName, string assetName, System.Type type,
            UnityAction<Object?> callback)
        {
            var coroutineId = ++_coroutineId;
            var coroutine = StartCoroutine(
                LoadAssetInternal(
                    assetBundleName,
                    assetName,
                    type,
                    callback,
                    true,
                    coroutineId
                )
            );
            _loadCoroutines.Add((assetBundleName, coroutineId), coroutine);
        }

        /// <summary>
        /// 同步卸载AB包
        /// </summary>
        /// <param name="assetBundleName">AB包名称</param>
        /// <param name="unloadAllLoadedObjects">是否卸载包含场景内的全部AB包资源</param>
        /// <param name="callback">卸载回调</param>
        public void Unload(string assetBundleName, bool unloadAllLoadedObjects, UnityAction callback)
        {
            StartCoroutine(UnloadInternal(assetBundleName, unloadAllLoadedObjects,
                false, callback));
        }

        /// <summary>
        /// 异步卸载AB包
        /// </summary>
        /// <param name="assetBundleName">AB包名称</param>
        /// <param name="unloadAllLoadedObjects">是否卸载包含场景内的全部AB包资源</param>
        /// <param name="callback">卸载回调</param>
        public void UnloadAsync(string assetBundleName, bool unloadAllLoadedObjects, UnityAction callback)
        {
            StartCoroutine(UnloadInternal(assetBundleName, unloadAllLoadedObjects,
                true, callback));
        }

        /// <summary>
        /// 强行卸载全部AB包，会清空全部缓存资源加载数据
        /// </summary>
        /// <param name="unloadAllObjects">是否卸载包含场景内的全部AB包资源</param>
        public void UnloadAllAssetBundles(bool unloadAllObjects)
        {
            // 停止正在加载的全部协程
            foreach (var keyValuePair in _loadCoroutines)
            {
                StopCoroutine(keyValuePair.Value);
            }

            // 清空加载记录
            _assetBundleLoads.Clear();
            // 卸载全部AB包
            AssetBundle.UnloadAllAssetBundles(unloadAllObjects);
            // 卸载主包
            _mainAssetBundle = null;
            _manifest = null;
        }

        private IEnumerator LoadAssetInternal(
            string assetBundleName,
            string assetName,
            System.Type type,
            UnityAction<Object?> callback,
            bool isAsync,
            uint coroutineId
        )
        {
            // 加载主包
            LoadMainAssetBundle();

            // 加载依赖包
            string[] dependencies = _manifest!.GetAllDependencies(assetBundleName);
            foreach (var dependency in dependencies)
            {
                yield return LoadAssetBundle(dependency, isAsync);
            }

            // 加载目标包
            yield return LoadAssetBundle(assetBundleName, isAsync);

            if (_assetBundleLoads.ContainsKey(assetBundleName) &&
                _assetBundleLoads[assetBundleName] is AssetBundleLoadComplete loadComplete)
            {
                if (!isAsync)
                {
                    callback?.Invoke(loadComplete.AssetBundle.LoadAsset(assetName, type));
                }
                else
                {
                    var loadAssetAsync = loadComplete.AssetBundle.LoadAssetAsync(assetName, type);
                    yield return loadAssetAsync;

                    callback?.Invoke(loadAssetAsync.asset);
                }
            }
            else
            {
                callback?.Invoke(null);
            }

            // 删除加载协程记录
            _loadCoroutines.Remove((assetBundleName, coroutineId));
        }

        /// <summary>
        /// 加载主包
        /// </summary>
        private void LoadMainAssetBundle()
        {
            if (_mainAssetBundle != null) return;
            _mainAssetBundle = AssetBundle.LoadFromFile(PrefixPath + mainAssetBundleName);
            _manifest = _mainAssetBundle.LoadAsset<AssetBundleManifest>("AssetBundleManifest");
        }

        /// <summary>
        /// 加载AB包
        /// </summary>
        /// <param name="assetBundleName"></param>
        /// <param name="isAsync"></param>
        /// <returns></returns>
        private IEnumerator LoadAssetBundle(string assetBundleName, bool isAsync)
        {
            // 判断AB包加载情况
            if (!_assetBundleLoads.ContainsKey(assetBundleName)) // AB包首次加载
            {
                if (!isAsync)
                {
                    var assetBundle = AssetBundle.LoadFromFile(PrefixPath + assetBundleName);
                    _assetBundleLoads.Add(assetBundleName, new AssetBundleLoadComplete(assetBundle));
                }
                else
                {
                    var abRequest = AssetBundle.LoadFromFileAsync(PrefixPath + assetBundleName);
                    _assetBundleLoads.Add(assetBundleName, new AssetBundleLoading(abRequest));
                    yield return abRequest;

                    // 判断协程切换回来后是否为卸载资源，是则卸载资源，否则就设置资源
                    if (_assetBundleLoads[assetBundleName] is AssetBundleUnload unload)
                    {
                        if (!unload.Async) // 同步卸载时
                        {
                            abRequest.assetBundle.Unload(unload.UnloadAllLoadedObjects);
                            unload.UnloadAction();
                            _assetBundleLoads.Remove(assetBundleName);
                        }
                        else // 异步卸载时
                        {
                            // 这里挂起直到异步卸载完成
                            var asyncOperation = abRequest.assetBundle.UnloadAsync(unload.UnloadAllLoadedObjects);
                            yield return asyncOperation;

                            unload.UnloadAction();
                            _assetBundleLoads.Remove(assetBundleName);
                        }
                    }
                    else
                    {
                        _assetBundleLoads[assetBundleName] = new AssetBundleLoadComplete(abRequest.assetBundle);
                    }
                }
            }
            else // AB包正在加载或加载完毕或卸载
            {
                // 如果处于正在加载或卸载状态则轮询至离开状态
                while (_assetBundleLoads.ContainsKey(assetBundleName) &&
                       (_assetBundleLoads[assetBundleName] is AssetBundleUnload ||
                        _assetBundleLoads[assetBundleName] is AssetBundleLoading))
                {
                    yield return 0;
                }

                // 走到这里的场景要么就是卸载结束要么就是加载结束
                // 最后判断资源是否被卸载，是则递归调用该方法加载资源
                if (!_assetBundleLoads.ContainsKey(assetBundleName))
                {
                    yield return LoadAssetBundle(assetBundleName, isAsync);
                }
            }
        }

        private IEnumerator UnloadInternal(string assetBundleName, bool unloadAllLoadedObjects, bool isAsync,
            UnityAction callback)
        {
            // 仅卸载已加载或正在加载的AB包
            if (_assetBundleLoads.ContainsKey(assetBundleName))
            {
                var assetBundleLoad = _assetBundleLoads[assetBundleName];
                if (assetBundleLoad is AssetBundleLoading) // 正在加载的AB包
                {
                    // 由AB包加载协程处理卸载
                    _assetBundleLoads[assetBundleName] =
                        new AssetBundleUnload(isAsync, unloadAllLoadedObjects, callback);
                }
                else if (assetBundleLoad is AssetBundleLoadComplete loadComplete) // 已加载完毕的AB包
                {
                    if (!isAsync) // 同步场景下
                    {
                        loadComplete.AssetBundle.Unload(unloadAllLoadedObjects);
                        callback?.Invoke();
                        _assetBundleLoads.Remove(assetBundleName);
                    }
                    else // 异步场景下
                    {
                        // 设置卸载数据，挂起直到卸载完成
                        var asyncOperation = loadComplete.AssetBundle.UnloadAsync(unloadAllLoadedObjects);
                        var unload = new AssetBundleUnload(true, unloadAllLoadedObjects, callback);
                        _assetBundleLoads[assetBundleName] = unload;
                        yield return asyncOperation;

                        callback?.Invoke();
                        _assetBundleLoads.Remove(assetBundleName);
                    }
                }
                else if (assetBundleLoad is AssetBundleUnload unload) // 等待卸载的AB包
                {
                    // 追加卸载回调
                    unload.UnloadAction += callback;
                }
            }
        }
    }
}