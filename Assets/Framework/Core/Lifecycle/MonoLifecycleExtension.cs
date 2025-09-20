using UnityEngine;
using UnityEngine.Events;

namespace Framework.Core.Lifecycle
{
    /// <summary>
    /// Mono生命周期拓展类
    /// </summary>
    public static class MonoLifecycleExtension
    {
        public static MonoLifecycle GetMonoLifecycle(this GameObject gameObject)
        {
            var monoLifecycle = gameObject.GetComponent<MonoLifecycle>();
            if (monoLifecycle == null)
            {
                monoLifecycle = gameObject.AddComponent<MonoLifecycleRegistry>();
            }

            return monoLifecycle;
        }

        public static void Observe(
            this MonoLifecycle monoLifecycle,
            LifecycleEvent lifecycleEvent,
            UnityAction observer
        )
        {
            monoLifecycle.AddObserver(new SimpleLifecycleObserver(lifecycleEvent, observer, false));
        }

        public static void ObserveOnce(
            this MonoLifecycle monoLifecycle,
            LifecycleEvent lifecycleEvent,
            UnityAction observer
        )
        {
            monoLifecycle.AddObserver(new SimpleLifecycleObserver(lifecycleEvent, observer, true));
        }

        private class SimpleLifecycleObserver : IMonoLifecycleObserver
        {
            private readonly LifecycleEvent _lifecycleEvent;
            private readonly UnityAction _observer;
            private readonly bool _observeOnce;

            public SimpleLifecycleObserver(LifecycleEvent lifecycleEvent, UnityAction observer, bool observeOnce)
            {
                _lifecycleEvent = lifecycleEvent;
                _observer = observer;
                _observeOnce = observeOnce;
            }

            public void OnStateChanged(MonoLifecycle monoLifecycle, LifecycleEvent lifecycleEvent)
            {
                if (lifecycleEvent == _lifecycleEvent)
                {
                    _observer.Invoke();
                    if (_observeOnce)
                    {
                        monoLifecycle.RemoveObserver(this);
                    }
                }
            }
        }
    }
}