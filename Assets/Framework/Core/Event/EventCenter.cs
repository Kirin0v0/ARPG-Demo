using System.Collections.Generic;
using UnityEngine.Events;

namespace Framework.Core.Event
{
    /// <summary>
    /// 事件中心模块，提供跨模块发布事件以及订阅事件的功能
    /// </summary>
    public class EventCenter: IEventRegistry, IEventTrigger
    {
        /// <summary>
        /// 事件委托基类
        /// </summary>
        private abstract class BaseEventAction
        {
        }

        /// <summary>
        /// 用来包裹对应泛型类型的委托
        /// </summary>
        /// <typeparam name="T">事件传递的参数</typeparam>
        private class EventAction<T> : BaseEventAction
        {
            public UnityAction<T> Actions;

            public EventAction(UnityAction<T> action)
            {
                Actions += action;
            }
        }

        /// <summary>
        /// 用来包裹无参无返回值委托
        /// </summary>
        private class EventAction : BaseEventAction
        {
            public UnityAction Actions;

            public EventAction(UnityAction action)
            {
                Actions += action;
            }
        }

        //用于记录对应事件关联的对应的逻辑
        private readonly Dictionary<EventIdentity, BaseEventAction> _eventActions = new();

        public EventCenter()
        {
        }

        /// <summary>
        /// 触发事件对应的监听器，无参传递，事件方调用
        /// </summary>
        /// <param name="eventIdentity"></param>
        public void TriggerEvent(EventIdentity eventIdentity)
        {
            if (_eventActions.ContainsKey(eventIdentity))
            {
                (_eventActions[eventIdentity] as EventAction)!.Actions?.Invoke();
            }
        }
        
        /// <summary>
        /// 触发事件对应的监听器，无参传递，事件方调用
        /// </summary>
        /// <param name="eventIdentity"></param>
        /// <param name="info"></param>
        public void TriggerEvent<T>(EventIdentity eventIdentity, T info)
        {
            if (_eventActions.ContainsKey(eventIdentity))
            {
                (_eventActions[eventIdentity] as EventAction<T>)!.Actions?.Invoke(info);
            }
        }

        public void AddEventListener(EventIdentity eventIdentity, UnityAction listener)
        {
            if (_eventActions.ContainsKey(eventIdentity))
            {
                (_eventActions[eventIdentity] as EventAction)!.Actions += listener;
            }
            else
            {
                _eventActions.Add(eventIdentity, new EventAction(listener));
            }
        }

        public void AddEventListener<T>(EventIdentity eventIdentity, UnityAction<T> listener)
        {
            if (_eventActions.ContainsKey(eventIdentity))
            {
                (_eventActions[eventIdentity] as EventAction<T>)!.Actions += listener;
            }
            else
            {
                _eventActions.Add(eventIdentity, new EventAction<T>(listener));
            }
        }

        public void RemoveEventListener(EventIdentity eventIdentity, UnityAction listener)
        {
            if (_eventActions.ContainsKey(eventIdentity))
                (_eventActions[eventIdentity] as EventAction)!.Actions -= listener;
        }

        public void RemoveEventListener<T>(EventIdentity eventIdentity, UnityAction<T> listener)
        {
            if (_eventActions.ContainsKey(eventIdentity))
                (_eventActions[eventIdentity] as EventAction<T>)!.Actions -= listener;
        }

        /// <summary>
        /// 清除全部事件的监听器
        /// </summary>
        public void Clear()
        {
            _eventActions.Clear();
        }

        /// <summary>
        /// 清除对应事件的监听器
        /// </summary>
        /// <param name="eventIdentity"></param>
        public void Clear(EventIdentity eventIdentity)
        {
            if (_eventActions.ContainsKey(eventIdentity))
                _eventActions.Remove(eventIdentity);
        }
    }
}