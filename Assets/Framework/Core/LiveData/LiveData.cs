using System;
using System.Collections.Generic;
using System.Threading;
using Framework.Core.Lifecycle;
using UnityEngine.Events;

namespace Framework.Core.LiveData
{
    public enum LiveDataMode
    {
        Default,
        Debounce,
    }
    
    /// <summary>
    /// 可监听数据类，用于在收紧权限（只读不可写）的场景
    /// </summary>
    /// <typeparam name="T">泛型类型</typeparam>
    public class LiveData<T>
    {
        private static readonly object NotSet = new object();

        private LiveDataMode _mode;
        
        private object _value;

        private readonly Dictionary<UnityAction<T>, ObserverWrapper> _observerMappings = new();

        public T Value => _value is T value ? value : default;

        public LiveData(LiveDataMode mode = LiveDataMode.Default)
        {
            _mode = mode;
            _value = NotSet;
        }

        public LiveData(T value, LiveDataMode mode = LiveDataMode.Default)
        {
            _mode = mode;
            _value = value;
        }

        public bool HasValue() => _value != NotSet;

        /// <summary>
        /// 根据Mono生命周期动态监听数据变化，监听节点是OnEnable->OnDestroy（此时会自动删除监听器）
        /// </summary>
        /// <param name="monoLifecycle">Mono生命周期</param>
        /// <param name="observer">监听回调</param>
        public void Observe(MonoLifecycle monoLifecycle, UnityAction<T> observer)
        {
            if (monoLifecycle.GetCurrentState() == LifecycleState.Destroyed)
            {
                return;
            }

            if (_observerMappings.ContainsKey(observer))
            {
                return;
            }

            var lifecycleObserver = new LifecycleObserver(this, observer, monoLifecycle);
            _observerMappings.Add(observer, lifecycleObserver);
            monoLifecycle.AddObserver(lifecycleObserver);
        }

        /// <summary>
        /// 永久监听数据变化
        /// </summary>
        /// <param name="observer">监听回调</param>
        public void ObserveForever(UnityAction<T> observer)
        {
            if (_observerMappings.ContainsKey(observer))
            {
                return;
            }

            var alwaysObserver = new AlwaysObserver(this, observer);
            _observerMappings.Add(observer, alwaysObserver);
            alwaysObserver.SetActive(true);
        }

        /// <summary>
        /// 删除监听器
        /// </summary>
        /// <param name="observer">注册时传入的监听回调</param>
        public void RemoveObserver(UnityAction<T> observer)
        {
            if (!_observerMappings.ContainsKey(observer))
            {
                return;
            }

            _observerMappings.Remove(observer);
        }

        protected void SetValue(T value)
        {
            // 如果防抖模式，就在这里去重防止抖动
            if (_mode == LiveDataMode.Debounce)
            {
                if ((_value == null && value == null) || (_value != null && _value.Equals(value)))
                {
                    return;
                }
            }

            _value = value;

            NotifyObservers();
        }

        private void NotifyObservers()
        {
            // 默认值不同步
            if (_value != null && _value.Equals(NotSet))
            {
                return;
            }

            // 非活跃监听器不同步数据变化
            foreach (var observerMappingsValue in _observerMappings.Values)
            {
                if (!observerMappingsValue.Active)
                {
                    continue;
                }

                if (!observerMappingsValue.ShouldBeActive())
                {
                    observerMappingsValue.SetActive(false);
                    continue;
                }

                observerMappingsValue.Observer.Invoke(Value);
            }
        }
        
        /// <summary>
        /// Mono生命周期监听器
        /// </summary>
        private class LifecycleObserver : ObserverWrapper, IMonoLifecycleObserver
        {
            private readonly MonoLifecycle _lifecycle;

            public LifecycleObserver(LiveData<T> liveData, UnityAction<T> observer, MonoLifecycle lifecycle) : base(
                liveData, observer)
            {
                _lifecycle = lifecycle;
            }

            public override bool ShouldBeActive()
            {
                return _lifecycle.GetCurrentState() >= LifecycleState.Enabled;
            }

            public void OnStateChanged(MonoLifecycle monoLifecycle, LifecycleEvent lifecycleEvent)
            {
                var currentState = monoLifecycle.GetCurrentState();
                if (currentState == LifecycleState.Destroyed)
                {
                    LiveData.RemoveObserver(Observer);
                    return;
                }

                SetActive(ShouldBeActive());
            }
        }

        /// <summary>
        /// 永久监听器
        /// </summary>
        private class AlwaysObserver : ObserverWrapper
        {
            public AlwaysObserver(LiveData<T> liveData, UnityAction<T> observer) : base(liveData, observer)
            {
            }

            public override bool ShouldBeActive()
            {
                return true;
            }
        }

        /// <summary>
        /// 监听包裹抽象类，规定基本函数
        /// </summary>
        private abstract class ObserverWrapper
        {
            public readonly LiveData<T> LiveData;
            public readonly UnityAction<T> Observer;

            public bool Active { get; private set; }

            protected ObserverWrapper(LiveData<T> liveData, UnityAction<T> observer)
            {
                LiveData = liveData;
                Observer = observer;
            }

            public abstract bool ShouldBeActive();

            public void SetActive(bool active)
            {
                if (Active == active)
                {
                    return;
                }

                Active = active;

                if (Active && !LiveData._value.Equals(NotSet))
                {
                    Observer.Invoke(LiveData.Value);
                }
            }
        }
    }
}