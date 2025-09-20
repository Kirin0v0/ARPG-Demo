using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.InputSystem;

namespace Inputs
{
    public class InputBuffer
    {
        private PlayerInput _playerInput;
        private float _timeout;
        private bool _onlyLastInputPerformed; // 仅最近一次输入执行生效

        private string _lastPerformedInputName;

        private readonly List<string> _inputNames = new();
        private readonly Dictionary<string, float> _inputPerformedBuffers = new();

        public void Init(PlayerInput playerInput, float timeout, bool onlyLastInputPerformed)
        {
            _playerInput = playerInput;
            _timeout = timeout;
            _onlyLastInputPerformed = onlyLastInputPerformed;
        }

        public void Register(string inputName)
        {
            _inputNames.Add(inputName);
            _playerInput.actions[inputName].performed += HandleInputPerformed;
            _playerInput.actions[inputName].canceled += HandleInputCanceled;
        }

        public void Unregister(string inputName)
        {
            _inputNames.Remove(inputName);
            _playerInput.actions[inputName].performed -= HandleInputPerformed;
            _playerInput.actions[inputName].canceled -= HandleInputCanceled;
        }

        public void Tick(float deltaTime)
        {
            for (int i = 0; i < _inputPerformedBuffers.Keys.Count; i++)
            {
                var key = _inputPerformedBuffers.Keys.ElementAt(i);
                _inputPerformedBuffers[key] -= deltaTime;
                if (_inputPerformedBuffers[key] <= 0f)
                {
                    _inputPerformedBuffers.Remove(key);
                }
            }
        }

        public bool WasPerformedThisFrame(string inputName)
        {
            if (_inputPerformedBuffers.TryGetValue(inputName, out var timeout))
            {
                if (_onlyLastInputPerformed)
                {
                    return _lastPerformedInputName == inputName && timeout > 0;
                }

                return timeout > 0;
            }

            return false;
        }

        public void Clear()
        {
            _inputPerformedBuffers.Clear();
        }

        public void Destroy()
        {
            _inputNames.Clear();
            _inputPerformedBuffers.Clear();
            _inputNames.ForEach(inputName =>
            {
                _playerInput.actions[inputName].performed -= HandleInputPerformed;
                _playerInput.actions[inputName].canceled -= HandleInputCanceled;
            });
        }

        private void HandleInputPerformed(InputAction.CallbackContext callbackContext)
        {
            var actionName = callbackContext.action.name;
            _inputPerformedBuffers[actionName] = float.MaxValue;
            if (_onlyLastInputPerformed)
            {
                _lastPerformedInputName = actionName;
            }
        }

        private void HandleInputCanceled(InputAction.CallbackContext callbackContext)
        {
            var actionName = callbackContext.action.name;
            _inputPerformedBuffers[actionName] = _timeout;
        }
    }
}