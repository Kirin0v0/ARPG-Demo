using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Framework.Common;

namespace Framework.Common.Input
{
    /// <summary>
    /// 基于Input系统封装的输入管理器，注意，鼠标相关输入存在与UI输入的冲突
    /// </summary>
    public class InputManager : MonoBehaviour
    {
        public event UnityAction<InputEventIdentity> TriggerInputEvent;   
        
        // 存放允许改键的自定义输入映射
        private readonly Dictionary<InputEventIdentity, BaseInputInfo> _customInputMappings = new();

        // 改键输入相关
        private bool _inChangeInputMapping;
        private InputEventIdentity _changeInputEvent;
        private UnityAction<BaseInputInfo> _changeInputMappingAction;

        // 是否开启输入控制
        public bool Enable { set; get; } = true;

        protected virtual void Update()
        {
            // 判断是否处于正在改键的状态，是则获取当前输入并执行回调，不参与后续的输入控制
            if (_inChangeInputMapping)
            {
                if (UnityEngine.Input.anyKeyDown)
                {
                    var inputAction = !_customInputMappings.ContainsKey(_changeInputEvent)
                        ? InputAction.Down
                        : _customInputMappings[_changeInputEvent].InputAction;

                    // 我们需要去遍历监听所有输入的按下，得到对应输入的信息
                    // 这里按照键盘（键盘支持多键输入）->鼠标（仅支持一键）来依次遍历，如果键盘有输入则不会流转到鼠标
                    var keyCodes = new List<KeyCode>();
                    foreach (KeyCode inputKey in Enum.GetValues(typeof(KeyCode)))
                    {
                        if (UnityEngine.Input.GetKeyDown(inputKey))
                        {
                            keyCodes.Add(inputKey);
                        }
                    }

                    if (keyCodes.Count != 0)
                    {
                        _changeInputMappingAction.Invoke(new KeyInputInfo(inputAction, keyCodes));
                        _changeInputEvent = null;
                        _changeInputMappingAction = null;
                        _inChangeInputMapping = false;
                        return;
                    }


                    for (int i = 0; i < 3; i++)
                    {
                        if (UnityEngine.Input.GetMouseButtonDown(i))
                        {
                            _changeInputMappingAction.Invoke(new MouseInputInfo(inputAction, i));
                            _changeInputEvent = null;
                            _changeInputMappingAction = null;
                            _inChangeInputMapping = false;
                            return;
                        }
                    }
                }

                return;
            }

            // 输入控制模块不可用则拦截输入监听和分发
            if (!Enable)
            {
                return;
            }

            // 监听和分发自定义输入映射的输入
            foreach (var inputMapping in _customInputMappings)
            {
                switch (inputMapping.Value.InputAction)
                {
                    case InputAction.Down:
                        if (inputMapping.Value is KeyInputInfo)
                        {
                            if (((KeyInputInfo)inputMapping.Value).KeyCodes.TrueForAll(UnityEngine.Input.GetKeyDown))
                            {
                                TriggerInputEvent?.Invoke(inputMapping.Key);
                            }
                        }
                        else if (inputMapping.Value is MouseInputInfo)
                        {
                            if (UnityEngine.Input.GetMouseButtonDown(((MouseInputInfo)inputMapping.Value).MouseId))
                            {
                                TriggerInputEvent?.Invoke(inputMapping.Key);
                            }
                        }

                        break;
                    case InputAction.Up:
                        if (inputMapping.Value is KeyInputInfo)
                        {
                            if (((KeyInputInfo)inputMapping.Value).KeyCodes.TrueForAll(UnityEngine.Input.GetKeyUp))
                            {
                                TriggerInputEvent?.Invoke(inputMapping.Key);
                            }
                        }
                        else if (inputMapping.Value is MouseInputInfo)
                        {
                            if (UnityEngine.Input.GetMouseButtonUp(((MouseInputInfo)inputMapping.Value).MouseId))
                            {
                                TriggerInputEvent?.Invoke(inputMapping.Key);
                            }
                        }

                        break;
                    case InputAction.LongPress:
                        if (inputMapping.Value is KeyInputInfo)
                        {
                            if (((KeyInputInfo)inputMapping.Value).KeyCodes.TrueForAll(UnityEngine.Input.GetKey))
                            {
                                TriggerInputEvent?.Invoke(inputMapping.Key);
                            }
                        }
                        else if (inputMapping.Value is MouseInputInfo)
                        {
                            if (UnityEngine.Input.GetMouseButton(((MouseInputInfo)inputMapping.Value).MouseId))
                            {
                                TriggerInputEvent?.Invoke(inputMapping.Key);
                            }
                        }

                        break;
                }
            }
        }

        public void ChangeInputMapping(InputEventIdentity inputEvent, BaseInputInfo inputInfo)
        {
            if (!_customInputMappings.ContainsKey(inputEvent))
            {
                _customInputMappings.Add(inputEvent, inputInfo);
            }
            else
            {
                _customInputMappings[inputEvent] = inputInfo;
            }
        }

        /// <summary>
        /// 获取输入映射
        /// </summary>
        /// <param name="inputMappings">输入映射</param>
        /// <returns></returns>
        public virtual void GetInputMappings(
            out Dictionary<InputEventIdentity, BaseInputInfo> inputMappings
        )
        {
            inputMappings = new();
            foreach (var inputMapping in _customInputMappings)
            {
                inputMappings.Add(inputMapping.Key, inputMapping.Value);
            }
        }

        public void BeginChangeInputMapping(InputEventIdentity inputEvent, UnityAction<BaseInputInfo> callback)
        {
            _changeInputEvent = inputEvent;
            _changeInputMappingAction = callback;
            StartCoroutine(BeginChangeInputMappingInternal());
        }
        
        private IEnumerator BeginChangeInputMappingInternal()
        {
            yield return 0;
            _inChangeInputMapping = true;
        }
    }
}