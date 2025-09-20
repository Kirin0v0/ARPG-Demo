using Framework.Core.Event;
using UnityEngine;
using UnityEngine.Events;

namespace Common
{
    public class EventManager: MonoBehaviour, IEventRegistry, IEventTrigger
    {
        private readonly EventCenter _eventCenter = new();
        
        public void AddEventListener(EventIdentity eventIdentity, UnityAction listener)
        {
            _eventCenter.AddEventListener(eventIdentity, listener);
        }

        public void AddEventListener<T>(EventIdentity eventIdentity, UnityAction<T> listener)
        {
            _eventCenter.AddEventListener(eventIdentity, listener);
        }

        public void RemoveEventListener(EventIdentity eventIdentity, UnityAction listener)
        {
            _eventCenter.RemoveEventListener(eventIdentity, listener);
        }

        public void RemoveEventListener<T>(EventIdentity eventIdentity, UnityAction<T> listener)
        {
            _eventCenter.RemoveEventListener(eventIdentity, listener);
        }

        public void TriggerEvent(EventIdentity eventIdentity)
        {
            _eventCenter.TriggerEvent(eventIdentity);
        }

        public void TriggerEvent<T>(EventIdentity eventIdentity, T info)
        {
            _eventCenter.TriggerEvent(eventIdentity, info);
        }
    }
}