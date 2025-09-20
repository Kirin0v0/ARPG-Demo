using System;
using System.Collections;
using System.Collections.Generic;
using Framework.Core.Singleton;
using Unity.VisualScripting;
using UnityEngine.AddressableAssets;
using UnityEngine.Events;
using UnityEngine.ResourceManagement.AsyncOperations;
using Object = UnityEngine.Object;

namespace Framework.Common.Resource
{
    public class AddressablesManager
    {
        private interface IAddressablesLoad
        {
            void Load();
            bool Release();
            void Clear();
            void Dispose();
        }

        private abstract class BaseAddressablesLoad : IAddressablesLoad
        {
            public readonly AsyncOperationHandle Handle;
            public uint Count { get; private set; }

            public bool IsLoading => !Handle.IsDone;
            public bool IsLoadSuccess => Handle.IsDone && Handle.Status == AsyncOperationStatus.Succeeded;

            protected BaseAddressablesLoad(AsyncOperationHandle handle)
            {
                Handle = handle;
            }

            public void Load()
            {
                Count++;
            }

            public bool Release()
            {
                Count = Math.Max(Count - 1, 0);
                return Count <= 0;
            }

            public void Clear()
            {
                Count = 0;
            }

            public abstract void Dispose();
        }

        private class AddressablesLoad<T> : BaseAddressablesLoad where T : Object
        {
            public event UnityAction<T> OnLoadSuccess;
            public event UnityAction OnLoadFailure;

            private readonly bool _listResult;

            public AddressablesLoad(AsyncOperationHandle handle, bool listResult) : base(handle)
            {
                _listResult = listResult;
                Handle.Completed += HandleCompleted;
            }

            public override void Dispose()
            {
                Handle.Completed -= HandleCompleted;
                if (IsLoading)
                {
                    OnLoadSuccess = null;
                    OnLoadFailure = null;
                }
                else
                {
                    Addressables.Release(Handle);
                }
            }

            private void HandleCompleted(AsyncOperationHandle operationHandle)
            {
                switch (operationHandle.Status)
                {
                    case AsyncOperationStatus.Succeeded:
                    {
                        // 根据资源是否是列表执行不同成功逻辑
                        if (_listResult)
                        {
                            var convert = Handle.Convert<IList<T>>();
                            foreach (var item in convert.Result)
                            {
                                OnLoadSuccess?.Invoke(item);
                            }
                        }
                        else
                        {
                            var convert = Handle.Convert<T>();
                            OnLoadSuccess?.Invoke(convert.Result);
                        }
                    }
                        break;
                    case AsyncOperationStatus.Failed:
                    {
                        OnLoadFailure?.Invoke();
                    }
                        break;
                }

                OnLoadSuccess = null;
                OnLoadFailure = null;
            }
        }

        // 缓存Addressables加载类的字典，key为[API查找key（如果存在多个用_分割）_资源类型]
        private readonly Dictionary<string, IAddressablesLoad> _addressablesLoads = new();

        /// <summary>
        /// 异步加载资源，如果资源重复加载了就记录加载次数+1
        /// </summary>
        /// <param name="key"></param>
        /// <param name="callback"></param>
        /// <typeparam name="T"></typeparam>
        public void LoadAssetAsync<T>(object key, UnityAction<T> callback) where T : Object
        {
            // 拼接key值
            var keyName = key.ToString() + "_" + typeof(T).Name;
            // 查找资源是否重复加载
            if (_addressablesLoads.TryGetValue(keyName, out var iAddressablesLoad))
            {
                if (iAddressablesLoad is not AddressablesLoad<T> addressablesLoad)
                {
                    UnityEngine.Debug.LogError($"{nameof(AddressablesManager)}资源获取失败: {keyName}");
                    return;
                }

                // 记录加载
                addressablesLoad.Load();
                // 判断该加载类是否加载结束
                if (!addressablesLoad.IsLoading)
                {
                    // 如果加载失败就直接跳过
                    if (!addressablesLoad.IsLoadSuccess)
                    {
                        return;
                    }

                    // 加载结束获取加载句柄并通过Convert转换为带有参数的句柄，直接调用回调
                    var convert = addressablesLoad.Handle.Convert<T>();
                    callback?.Invoke(convert.Result);
                }
                else
                {
                    addressablesLoad.OnLoadSuccess += callback;
                }

                return;
            }

            // 如果没有加载过该资源，直接进行异步加载
            var handle = Addressables.LoadAssetAsync<T>(key);
            // 记录加载
            var load = new AddressablesLoad<T>(handle, false);
            load.Load();
            load.OnLoadSuccess += callback;
            load.OnLoadFailure += () =>
            {
                // 加载失败则抹除加载记录，允许后续重新加载
                UnityEngine.Debug.LogWarning($"{nameof(AddressablesManager)}资源加载失败: {keyName}");
                load.Dispose();
                _addressablesLoads.Remove(keyName);
            };
            _addressablesLoads.Add(keyName, load);
        }

        /// <summary>
        /// 异步加载资源，如果资源重复加载了就记录加载次数+1
        /// </summary>
        /// <param name="keys"></param>
        /// <param name="callback"></param>
        /// <param name="mode"></param>
        /// <param name="releaseDependenciesOnFailure"></param>
        /// <typeparam name="T"></typeparam>
        public void LoadAssetsAsync<T>(
            IEnumerable keys,
            UnityAction<T> callback,
            Addressables.MergeMode mode,
            bool releaseDependenciesOnFailure
        ) where T : Object
        {
            // 拼接key值
            var keyName = keys.ToSeparatedString("_") + "_" + typeof(T).Name;
            // 查找资源是否重复加载
            if (_addressablesLoads.TryGetValue(keyName, out var iAddressablesLoad))
            {
                if (iAddressablesLoad is not AddressablesLoad<T> addressablesLoad)
                {
                    UnityEngine.Debug.LogError($"{nameof(AddressablesManager)}资源获取失败: {keyName}");
                    return;
                }

                // 记录加载
                addressablesLoad.Load();
                // 判断该加载类是否加载结束
                if (!addressablesLoad.IsLoading)
                {
                    // 如果加载失败就直接跳过
                    if (!addressablesLoad.IsLoadSuccess)
                    {
                        return;
                    }

                    // 加载结束获取加载句柄并通过Convert转换为带有参数的句柄
                    var convert = addressablesLoad.Handle.Convert<IList<T>>();
                    // 加载结束遍历调用回调
                    foreach (var item in convert.Result)
                    {
                        callback?.Invoke(item);
                    }
                }
                else
                {
                    addressablesLoad.OnLoadSuccess += callback;
                }

                return;
            }

            // 如果没有加载过该资源，直接进行异步加载
            var handle = Addressables.LoadAssetsAsync<T>(keys, null, mode, releaseDependenciesOnFailure);
            // 记录加载
            var load = new AddressablesLoad<T>(handle, true);
            load.Load();
            load.OnLoadSuccess += callback;
            load.OnLoadFailure += () =>
            {
                // 加载失败则抹除加载记录，允许后续重新加载
                UnityEngine.Debug.LogWarning($"{nameof(AddressablesManager)}资源加载失败: {keyName}");
                load.Dispose();
                _addressablesLoads.Remove(keyName);
            };
            _addressablesLoads.Add(keyName, load);
        }

        /// <summary>
        /// 释放指定资源，默认仅让加载记录-1，如果此时加载记录为0（代表任何场景都无该资源加载引用），则释放该资源
        /// </summary>
        /// <param name="key"></param>
        /// <param name="callback"></param>
        /// <typeparam name="T"></typeparam>
        public void ReleaseAsset<T>(object key, UnityAction callback = null) where T : Object
        {
            var keyName = key.ToString() + "_" + typeof(T).Name;
            ReleaseAssetInternal(keyName, callback);
        }

        /// <summary>
        /// 释放指定资源，默认仅让加载记录-1，如果此时加载记录为0（代表任何场景都无该资源加载引用），则释放该资源
        /// </summary>
        /// <param name="key"></param>
        /// <param name="type"></param>
        /// <param name="callback"></param>
        public void ReleaseAsset(object key, Type type, UnityAction callback = null)
        {
            var keyName = key.ToString() + "_" + type.Name;
            ReleaseAssetInternal(keyName, callback);
        }

        /// <summary>
        /// 释放指定资源，默认仅让加载记录-1，如果此时加载记录为0（代表任何场景都无该资源加载引用），则释放该资源
        /// </summary>
        /// <param name="keys"></param>
        /// <param name="callback"></param>
        /// <typeparam name="T"></typeparam>
        public void ReleaseAssets<T>(IEnumerable keys, UnityAction callback = null) where T : Object
        {
            var keyName = keys.ToSeparatedString("_") + "_" + typeof(T).Name;
            ReleaseAssetInternal(keyName, callback);
        }

        /// <summary>
        /// 释放指定资源，默认仅让加载记录-1，如果此时加载记录为0（代表任何场景都无该资源加载引用），则释放该资源
        /// </summary>
        /// <param name="keys"></param>
        /// <param name="type"></param>
        /// <param name="callback"></param>
        public void ReleaseAssets(IEnumerable keys, Type type, UnityAction callback = null)
        {
            var keyName = keys.ToSeparatedString("_") + "_" + type.Name;
            ReleaseAssetInternal(keyName, callback);
        }

        private void ReleaseAssetInternal(string pathName, UnityAction callback = null)
        {
            // 查找资源是否存在
            if (_addressablesLoads.TryGetValue(pathName, out var iAddressablesLoad))
            {
                if (iAddressablesLoad is not BaseAddressablesLoad addressablesLoad)
                {
                    UnityEngine.Debug.LogError($"{nameof(AddressablesManager)}资源获取失败: {pathName}");
                    return;
                }

                // 释放加载
                if (addressablesLoad.Release())
                {
                    // 移除加载
                    addressablesLoad.Dispose();
                    _addressablesLoads.Remove(pathName);
                    callback?.Invoke();
                }
            }
            else
            {
                callback?.Invoke();
            }
        }

        /// <summary>
        /// 清空所有已加载的资源
        /// </summary>
        public void ClearAllAssets()
        {
            foreach (var iAddressablesLoad in _addressablesLoads.Values)
            {
                if (iAddressablesLoad is not BaseAddressablesLoad addressablesLoad)
                {
                    continue;
                }

                // 清空加载
                addressablesLoad.Clear();
                addressablesLoad.Dispose();
            }

            _addressablesLoads.Clear();
        }
    }
}