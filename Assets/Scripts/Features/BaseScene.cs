using System;
using System.Collections.Generic;
using Events;
using Events.Data;
using Framework.Common.Debug;
using UnityEngine;
using UnityEngine.Events;
using Object = UnityEngine.Object;

namespace Features
{
    /// <summary>
    /// 场景基类
    /// </summary>
    public abstract class BaseScene : MonoBehaviour
    {
        /// <summary>
        /// Addressables资源临时加载列表，设计是为了场景资源的快速加载，不需要关心资源的释放，在场景销毁时自动释放
        /// </summary>
        private readonly List<(Type type, string key)> _addressablesLoads = new();

        private void Awake()
        {
            DebugUtil.LogGrey($"场景({GetType().Name})初始化");
            // 添加前往场景的事件监听器
            GameApplication.Instance.EventCenter.AddEventListener<GotoSceneEventParameter>(GameEvents.BeforeGotoScene,
                OnGotoSceneBefore);
            GameApplication.Instance.EventCenter.AddEventListener<GotoSceneEventParameter>(GameEvents.AfterGotoScene,
                OnGotoSceneAfter);
            OnAwake();
        }

        private void OnDestroy()
        {
            DebugUtil.LogGrey($"场景({GetType().Name})销毁");
            OnDispose();
            // 删除前往场景的事件监听器
            GameApplication.Instance?.EventCenter.RemoveEventListener<GotoSceneEventParameter>(GameEvents.BeforeGotoScene,
                OnGotoSceneBefore);
            GameApplication.Instance?.EventCenter.RemoveEventListener<GotoSceneEventParameter>(GameEvents.AfterGotoScene,
                OnGotoSceneAfter);
            // 清空临时资源加载
            _addressablesLoads.ForEach(tuple =>
            {
                GameApplication.Instance?.AddressablesManager.ReleaseAsset(tuple.key, tuple.type);
            });
            _addressablesLoads.Clear();
            // 清空全局事件中心（这个是兜底，避免出现忘记解除注册导致的内存泄漏）
            GameApplication.Instance?.EventCenter.Clear();
        }

        protected virtual void OnAwake()
        {
            
        }

        protected virtual void OnDispose()
        {
            
        }

        public void LoadAssetAsyncTemporary<T>(object key, UnityAction<T> callback) where T : Object
        {
            _addressablesLoads.Add(new() { type = typeof(T), key = key.ToString() });
            GameApplication.Instance.AddressablesManager.LoadAssetAsync<T>(key, callback);
        }

        public void ReleaseAssetTemporary<T>(object key, UnityAction callback = null) where T : Object
        {
            if (_addressablesLoads.Remove(new() { type = typeof(T), key = key.ToString() }))
            {
                GameApplication.Instance.AddressablesManager.ReleaseAsset<T>(key, callback);
            }
        }

        protected virtual void OnGotoSceneBefore(GotoSceneEventParameter parameter)
        {
        }

        protected virtual void OnGotoSceneAfter(GotoSceneEventParameter parameter)
        {
        }
    }
}