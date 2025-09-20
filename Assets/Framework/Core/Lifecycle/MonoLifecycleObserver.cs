namespace Framework.Core.Lifecycle
{
    /// <summary>
    /// Mono生命周期监听器接口
    /// </summary>
    public interface IMonoLifecycleObserver
    {
        public void OnStateChanged(MonoLifecycle monoLifecycle, LifecycleEvent lifecycleEvent);
    }
}