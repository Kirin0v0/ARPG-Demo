using System;
using Framework.Common.Debug;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.DualShock;
using UnityEngine.InputSystem.Switch;
using UnityEngine.InputSystem.XInput;
using VContainer;

namespace Inputs
{
    [Flags]
    public enum InputDeviceType
    {
        KeyboardAndMouse = 1 << 0,
        DualShockGamepad = 1 << 1,
        XboxGamepad = 1 << 2,
        NintendoSwitchGamepad = 1 << 3,
    }

    public class InputInfoManager : MonoBehaviour
    {
        private InputDeviceType _inputDeviceType;

        public InputDeviceType InputDeviceType
        {
            private set
            {
                if (value != _inputDeviceType)
                {
                    _inputDeviceType = value;
                    OnInputDeviceChanged?.Invoke(_inputDeviceType);
                }
            }
            get => _inputDeviceType;
        }

        public event System.Action<InputDeviceType> OnInputDeviceChanged;

        private void Awake()
        {
            _inputDeviceType = InputDeviceType.KeyboardAndMouse;
        }

        private void OnEnable()
        {
            InputSystem.onActionChange += HandleSwitchInputDevice;
        }

        private void OnDisable()
        {
            InputSystem.onActionChange -= HandleSwitchInputDevice;
        }

        private void HandleSwitchInputDevice(object obj, InputActionChange inputActionChange)
        {
            if (inputActionChange != InputActionChange.BoundControlsChanged) return;
            var devices = obj switch
            {
                InputActionAsset => ((InputActionAsset)obj).devices,
                InputActionMap => ((InputActionMap)obj).devices,
                _ => null
            };
            if (devices.HasValue && devices.Value.Count > 0)
            {
                var device = devices.Value[0];
                InputDeviceType = device switch
                {
                    DualShockGamepad => InputDeviceType.DualShockGamepad,
                    XInputController => InputDeviceType.XboxGamepad,
                    SwitchProControllerHID => InputDeviceType.NintendoSwitchGamepad,
                    _ => InputDeviceType.KeyboardAndMouse
                };
            }
            else
            {
                InputDeviceType = InputDeviceType.KeyboardAndMouse;
            }
        }
    }
}