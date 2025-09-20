using UnityEngine.Events;

namespace Framework.Core.Event
{
    public interface IEventRegistry
    {
        public void AddEventListener(EventIdentity eventIdentity, UnityAction listener);
        public void AddEventListener<T>(EventIdentity eventIdentity, UnityAction<T> listener);
        public void RemoveEventListener(EventIdentity eventIdentity, UnityAction listener);
        public void RemoveEventListener<T>(EventIdentity eventIdentity, UnityAction<T> listener);
    }
}