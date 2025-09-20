using System;
using System.Collections;
using System.Collections.Generic;
using Framework.Common.Debug;
using Framework.Core.Singleton;
using Sirenix.Utilities;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Inputs
{
    /// <summary>
    /// PlayerInput管理器，解决以下问题：
    /// 1.解决PlayerInput在InvokeCSharpEvents时的注册/解除注册事件问题（问题背景：PlayerInput在OnDisable/OnDestroy时会置空当前map，导致无法获取按键进行解除注册事件）
    /// 2.解决PlayerInput切换Map后同名按键的事件丢失问题（问题背景：Player在切换后同名InputAction本质上是两个对象，之前注册的事件无法被回调到）
    /// </summary>
    [RequireComponent(typeof(PlayerInput))]
    public class PlayerInputManager : MonoBehaviour
    {
        [SerializeField] private string defaultActionMap;

        private PlayerInput _playerInput;

        public PlayerInput PlayerInput
        {
            get
            {
                if (!_playerInput)
                {
                    _playerInput = GetComponent<PlayerInput>();
                    _playerInput.notificationBehavior = PlayerNotifications.InvokeCSharpEvents;
                }

                return _playerInput;
            }
        }

        private readonly Dictionary<string, PlayerInputActionEvent> _actionStartedEvents = new();
        private readonly Dictionary<string, PlayerInputActionEvent> _actionPerformedEvents = new();
        private readonly Dictionary<string, PlayerInputActionEvent> _actionCanceledEvents = new();

        private void Awake()
        {
            SwitchCurrentActionMap(defaultActionMap);
        }

        private void OnDestroy()
        {
            // 清空事件委托
            _actionStartedEvents.Values.ForEach(actionEvent => actionEvent.Delegates.Clear());
            _actionPerformedEvents.Values.ForEach(actionEvent => actionEvent.Delegates.Clear());
            _actionCanceledEvents.Values.ForEach(actionEvent => actionEvent.Delegates.Clear());

            // 清空字典
            _actionStartedEvents.Clear();
            _actionPerformedEvents.Clear();
            _actionCanceledEvents.Clear();
        }

        public void SwitchCurrentActionMap(string mapName)
        {
            // 过滤重复切换
            if (PlayerInput.currentActionMap != null && PlayerInput.currentActionMap.name == mapName)
            {
                return;
            }

            PlayerInput.SwitchCurrentActionMap(mapName);

            // 这里切换后重置回调
            _actionStartedEvents.ForEach(pair =>
            {
                ResetActionEvent(pair.Key, pair.Value, PlayerInputActionEventType.Started);
            });
            _actionPerformedEvents.ForEach(pair =>
            {
                ResetActionEvent(pair.Key, pair.Value, PlayerInputActionEventType.Performed);
            });
            _actionCanceledEvents.ForEach(pair =>
            {
                ResetActionEvent(pair.Key, pair.Value, PlayerInputActionEventType.Canceled);
            });

            return;

            void ResetActionEvent(string actionName, PlayerInputActionEvent actionEvent,
                PlayerInputActionEventType eventType)
            {
                var inputAction = GetInputAction(actionName);
                if (inputAction == null) return;
                // 如果已注册则检查当前回调列表是否为空，是则解除注册，如果没有注册则检查当前回调列表是否不为空，是则注册
                if (actionEvent.Maps.Contains(inputAction.actionMap.name))
                {
                    if (actionEvent.Delegates.Count <= 0)
                    {
                        actionEvent.Maps.Remove(inputAction.actionMap.name);
                        switch (eventType)
                        {
                            case PlayerInputActionEventType.Started:
                                inputAction.started -= actionEvent.HandleEvent;
                                break;
                            case PlayerInputActionEventType.Performed:
                                inputAction.performed -= actionEvent.HandleEvent;
                                break;
                            case PlayerInputActionEventType.Canceled:
                                inputAction.canceled -= actionEvent.HandleEvent;
                                break;
                        }
                    }
                }
                else
                {
                    if (actionEvent.Delegates.Count > 0)
                    {
                        actionEvent.Maps.Add(inputAction.actionMap.name);
                        switch (eventType)
                        {
                            case PlayerInputActionEventType.Started:
                                inputAction.started += actionEvent.HandleEvent;
                                break;
                            case PlayerInputActionEventType.Performed:
                                inputAction.performed += actionEvent.HandleEvent;
                                break;
                            case PlayerInputActionEventType.Canceled:
                                inputAction.canceled += actionEvent.HandleEvent;
                                break;
                        }
                    }
                }
            }
        }

        public InputAction GetInputAction(string actionName)
        {
            return PlayerInput.currentActionMap?.FindAction(actionName);
        }

        public bool WasPerformedThisFrame(string actionName)
        {
            var inputAction = PlayerInput.currentActionMap.FindAction(actionName);
            return inputAction != null && inputAction.WasPerformedThisFrame();
        }

        public bool IsPressed(string actionName)
        {
            var inputAction = PlayerInput.currentActionMap.FindAction(actionName);
            return inputAction != null && inputAction.IsPressed();
        }

        public void RegisterActionStarted(string actionName, Action<InputAction.CallbackContext> action)
        {
            RegisterActionEvent(actionName, action, PlayerInputActionEventType.Started);
        }

        public void UnregisterActionStarted(string actionName, Action<InputAction.CallbackContext> action)
        {
            UnregisterActionEvent(actionName, action, PlayerInputActionEventType.Started);
        }

        public void RegisterActionPerformed(string actionName, Action<InputAction.CallbackContext> action)
        {
            RegisterActionEvent(actionName, action, PlayerInputActionEventType.Performed);
        }

        public void UnregisterActionPerformed(string actionName, Action<InputAction.CallbackContext> action)
        {
            UnregisterActionEvent(actionName, action, PlayerInputActionEventType.Performed);
        }

        public void RegisterActionCanceled(string actionName, Action<InputAction.CallbackContext> action)
        {
            RegisterActionEvent(actionName, action, PlayerInputActionEventType.Canceled);
        }

        public void UnregisterActionCanceled(string actionName, Action<InputAction.CallbackContext> action)
        {
            UnregisterActionEvent(actionName, action, PlayerInputActionEventType.Canceled);
        }

        private void RegisterActionEvent(string actionName, Action<InputAction.CallbackContext> action,
            PlayerInputActionEventType eventType)
        {
            var events = eventType switch
            {
                PlayerInputActionEventType.Started => _actionStartedEvents,
                PlayerInputActionEventType.Performed => _actionPerformedEvents,
                PlayerInputActionEventType.Canceled => _actionCanceledEvents,
            };
            if (!events.TryGetValue(actionName, out var actionEvent))
            {
                actionEvent = new PlayerInputActionEvent();
                events.Add(actionName, actionEvent);
            }

            if (actionEvent.Delegates.Contains(action))
            {
                return;
            }

            actionEvent.Delegates.Add(action);

            var inputAction = GetInputAction(actionName);
            if (inputAction != null && !actionEvent.Maps.Contains(inputAction.actionMap.name) &&
                actionEvent.Delegates.Count > 0)
            {
                actionEvent.Maps.Add(inputAction.actionMap.name);
                switch (eventType)
                {
                    case PlayerInputActionEventType.Started:
                        inputAction.started += actionEvent.HandleEvent;
                        break;
                    case PlayerInputActionEventType.Performed:
                        inputAction.performed += actionEvent.HandleEvent;
                        break;
                    case PlayerInputActionEventType.Canceled:
                        inputAction.canceled += actionEvent.HandleEvent;
                        break;
                }
            }
        }

        private void UnregisterActionEvent(string actionName, Action<InputAction.CallbackContext> action,
            PlayerInputActionEventType eventType)
        {
            var events = eventType switch
            {
                PlayerInputActionEventType.Started => _actionStartedEvents,
                PlayerInputActionEventType.Performed => _actionPerformedEvents,
                PlayerInputActionEventType.Canceled => _actionCanceledEvents,
            };
            if (!events.TryGetValue(actionName, out var actionEvent))
            {
                return;
            }

            if (!actionEvent.Delegates.Contains(action))
            {
                return;
            }

            actionEvent.Delegates.Remove(action);

            var inputAction = GetInputAction(actionName);
            if (inputAction != null && actionEvent.Maps.Contains(inputAction.actionMap.name) &&
                actionEvent.Delegates.Count <= 0)
            {
                actionEvent.Maps.Remove(PlayerInput.currentActionMap.name);
                switch (eventType)
                {
                    case PlayerInputActionEventType.Started:
                        inputAction.started -= actionEvent.HandleEvent;
                        break;
                    case PlayerInputActionEventType.Performed:
                        inputAction.performed -= actionEvent.HandleEvent;
                        break;
                    case PlayerInputActionEventType.Canceled:
                        inputAction.canceled -= actionEvent.HandleEvent;
                        break;
                }
            }
        }

        private enum PlayerInputActionEventType
        {
            Started,
            Performed,
            Canceled,
        }

        private class PlayerInputActionEvent
        {
            public readonly List<string> Maps = new();
            public readonly List<Action<InputAction.CallbackContext>> Delegates = new();

            public void HandleEvent(InputAction.CallbackContext callbackContext)
            {
                Delegates.ToArray().ForEach(d => d.Invoke(callbackContext));
            }
        }
    }
}