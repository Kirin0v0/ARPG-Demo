using System;
using System.ComponentModel;
using Framework.Core.Attribute;
using UnityEngine;
using UnityEngine.Events;

namespace Framework.Core.Lifecycle
{
    /// <summary>
    /// Mono生命周期状态枚举
    /// </summary>
    public enum LifecycleState
    {
        Destroyed,
        Initialed,
        Awoken,
        Enabled,
        Started,
    }

    /// <summary>
    /// Mono生命周期事件枚举
    /// </summary>
    public enum LifecycleEvent
    {
        OnAwake,
        OnEnable,
        OnStart,
        OnDisable,
        OnDestroy,
    }
    
    /// <summary>
    /// Mono生命周期抽象类
    /// </summary>
    public abstract class MonoLifecycle : MonoBehaviour
    {
        public abstract void AddObserver(IMonoLifecycleObserver observer);
        
        public abstract void RemoveObserver(IMonoLifecycleObserver observer);

        public abstract LifecycleState GetCurrentState();
    }
}