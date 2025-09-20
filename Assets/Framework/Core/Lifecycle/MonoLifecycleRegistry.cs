using System.Collections.Generic;
using System.ComponentModel;
using Framework.Core.Attribute;
using UnityEngine;

namespace Framework.Core.Lifecycle
{
    /// <summary>
    /// Mono生命周期注册类，即真正的实现类
    /// </summary>
    public class MonoLifecycleRegistry : MonoLifecycle
    {
        // 状态仅显示在Inspector面板上
        [DisplayOnly] [SerializeField] private LifecycleState currentState = LifecycleState.Initialed;

        // 监听器列表
        private readonly List<IMonoLifecycleObserver> _lifecycleObserverList = new();

        // 累计事件栈（存在重新经历向上节点的弹栈场景）
        private readonly Stack<LifecycleEvent> _lifecycleEventStack = new();

        private void Awake()
        {
            HandleLifecycleEvent(LifecycleEvent.OnAwake);
        }

        private void OnEnable()
        {
            HandleLifecycleEvent(LifecycleEvent.OnEnable);
        }

        private void Start()
        {
            HandleLifecycleEvent(LifecycleEvent.OnStart);
        }

        private void OnDisable()
        {
            HandleLifecycleEvent(LifecycleEvent.OnDisable);
        }

        private void OnDestroy()
        {
            HandleLifecycleEvent(LifecycleEvent.OnDestroy);
        }

        public override void AddObserver(IMonoLifecycleObserver observer)
        {
            _lifecycleObserverList.Add(observer);
            // 新添加监听器会同步当前的事件栈
            foreach (var lifecycleEvent in _lifecycleEventStack)
            {
                observer.OnStateChanged(this, lifecycleEvent);
            }
        }

        public override void RemoveObserver(IMonoLifecycleObserver observer)
        {
            _lifecycleObserverList.Remove(observer);
        }

        public override LifecycleState GetCurrentState()
        {
            return currentState;
        }

        private void HandleLifecycleEvent(LifecycleEvent lifecycleEvent)
        {
            var state = GetStateAfterEvent(lifecycleEvent);
            MoveToState(state);
        }

        private void MoveToState(LifecycleState lifecycleState)
        {
            var previousState = currentState;
            if (previousState == lifecycleState)
            {
                return;
            }

            currentState = lifecycleState;

            // 根据状态改变事件栈
            if (lifecycleState > previousState)
            {
                // 计算向上事件
                var lifecycleEvent = GetEventUpToState(lifecycleState);
                if (lifecycleEvent != null)
                {
                    // 同步事件
                    _lifecycleObserverList.ForEach(observer =>
                        observer.OnStateChanged(this, (LifecycleEvent)lifecycleEvent));
                    while (_lifecycleEventStack.TryPeek(out var result) && result >= lifecycleEvent)
                    {
                        _lifecycleEventStack.Pop();
                    }

                    _lifecycleEventStack.Push((LifecycleEvent)lifecycleEvent);
                }
            }
            else
            {
                // 计算向下事件
                var lifecycleEvent = GetEventDownToState(lifecycleState);
                if (lifecycleEvent != null)
                {
                    // 同步事件
                    _lifecycleObserverList.ForEach(observer =>
                        observer.OnStateChanged(this, (LifecycleEvent)lifecycleEvent));
                    _lifecycleEventStack.Push((LifecycleEvent)lifecycleEvent);
                }
            }

            // GameObject销毁时清空监听器
            if (lifecycleState == LifecycleState.Destroyed)
            {
                _lifecycleObserverList.Clear();
            }
        }

        /// <summary>
        /// 获取事件之后的状态
        /// </summary>
        /// <param name="lifecycleEvent">事件</param>
        /// <returns>之后的状态</returns>
        /// <exception cref="InvalidEnumArgumentException"></exception>
        private LifecycleState GetStateAfterEvent(LifecycleEvent lifecycleEvent)
        {
            switch (lifecycleEvent)
            {
                case LifecycleEvent.OnAwake:
                case LifecycleEvent.OnDisable:
                    return LifecycleState.Awoken;
                case LifecycleEvent.OnEnable:
                    return LifecycleState.Enabled;
                case LifecycleEvent.OnStart:
                    return LifecycleState.Started;
                case LifecycleEvent.OnDestroy:
                    return LifecycleState.Destroyed;
            }

            throw new InvalidEnumArgumentException("Unexpected event value: " + lifecycleEvent);
        }

        /// <summary>
        /// 获取指向该状态的向上事件
        /// </summary>
        /// <param name="lifecycleState">状态</param>
        /// <returns></returns>
        private LifecycleEvent? GetEventUpToState(LifecycleState lifecycleState)
        {
            switch (lifecycleState)
            {
                case LifecycleState.Awoken:
                    return LifecycleEvent.OnAwake;
                case LifecycleState.Enabled:
                    return LifecycleEvent.OnEnable;
                case LifecycleState.Started:
                    return LifecycleEvent.OnStart;
            }

            return null;
        }

        /// <summary>
        /// 获取指向该状态的向下事件
        /// </summary>
        /// <param name="lifecycleState">状态</param>
        /// <returns></returns>
        private LifecycleEvent? GetEventDownToState(LifecycleState lifecycleState)
        {
            switch (lifecycleState)
            {
                case LifecycleState.Awoken:
                    return LifecycleEvent.OnDisable;
                case LifecycleState.Destroyed:
                    return LifecycleEvent.OnDestroy;
            }

            return null;
        }
    }
}