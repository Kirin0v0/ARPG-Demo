using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Framework.Common.Util;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;

namespace Framework.Common.Input
{
    /// <summary>
    /// 基于InputSystem系统封装的输入管理器，使用前需要提前准备好InputActions的json文件并设置好初始值
    /// </summary>
    public class InputSystemManager
    {
        // InputActions json文件位置
        private const string FilePath = "InputActions";

        // 输入映射字典
        private readonly Dictionary<string, string> _inputMappings = new();

        // PlayerInput组件列表，用于存储绑定输入按键的PlayerInput
        private readonly List<PlayerInput> _playerInputs = new();

        private InputSystemManager()
        {
            SetDefaultMappings();
        }

        /// <summary>
        /// 绑定PlayerInput组件，之后按键更改都会更新PlayerInput
        /// </summary>
        /// <param name="playerInput">PlayerInput组件</param>
        public void Bind(PlayerInput playerInput)
        {
            if (!_playerInputs.Contains(playerInput))
            {
                _playerInputs.Add(playerInput);
            }

            var jsonString = Resources.Load<TextAsset>(FilePath).text;
            foreach (var keyValuePair in _inputMappings)
            {
                jsonString = jsonString.Replace(keyValuePair.Key, keyValuePair.Value);
            }

            playerInput.actions = InputActionAsset.FromJson(jsonString);
            playerInput.actions.Enable();
        }

        /// <summary>
        /// 解绑PlayerInput组件
        /// </summary>
        /// <param name="playerInput">PlayerInput组件</param>
        public void Unbind(PlayerInput playerInput)
        {
            if (_playerInputs.Contains(playerInput))
            {
                _playerInputs.Remove(playerInput);
            }

            playerInput.actions.Disable();
            playerInput.actions = null;
        }

        /// <summary>
        /// 改变按键
        /// </summary>
        /// <param name="keyName">按键名称</param>
        /// <param name="afterChangeCallback">改变后的回调</param>
        public void ChangeKey(string keyName, UnityAction afterChangeCallback)
        {
            foreach (var playerInput in _playerInputs)
            {
                playerInput.actions.Disable();
            }

            UnityEngine.InputSystem.InputSystem.onAnyButtonPress.CallOnce((control =>
            {
                var strs = control.path.Split("/", 2, StringSplitOptions.RemoveEmptyEntries);
                var path = "<" + strs[0] + ">/" + strs[1];
                SetMapping(keyName, path);
                foreach (var playerInput in _playerInputs)
                {
                    Bind(playerInput);
                }

                afterChangeCallback.Invoke();
            }));
        }

        /// <summary>
        /// 获取对应按键的路径
        /// </summary>
        /// <param name="keyName">按键名称</param>
        /// <returns>按键路径</returns>
        public string GetKeyPath(string keyName)
        {
            if (_inputMappings.ContainsKey(keyName))
            {
                return _inputMappings[keyName];
            }

            return "";
        }

        private void SetDefaultMappings()
        {
            // 在这里初始化默认按键
            SetMapping(InputSystemConstants.Up, GetDefaultMapping(InputSystemConstants.Up, "<Keyboard>/w"));
            SetMapping(InputSystemConstants.Down, GetDefaultMapping(InputSystemConstants.Down, "<Keyboard>/s"));
            SetMapping(InputSystemConstants.Left, GetDefaultMapping(InputSystemConstants.Left, "<Keyboard>/a"));
            SetMapping(InputSystemConstants.Right, GetDefaultMapping(InputSystemConstants.Right, "<Keyboard>/d"));
            SetMapping(InputSystemConstants.Fire, GetDefaultMapping(InputSystemConstants.Fire, "<Mouse>/leftButton"));
            SetMapping(InputSystemConstants.Jump, GetDefaultMapping(InputSystemConstants.Jump, "<Keyboard>/space"));
        }

        private void SetMapping(string key, string value)
        {
            if (!_inputMappings.TryAdd(key, value))
            {
                _inputMappings[key] = value;
            }

            PlayerPrefsUtil.SaveData(key, value);
        }

        private string GetDefaultMapping(string keyName, string defaultValue)
        {
            var path = PlayerPrefsUtil.LoadData(keyName, typeof(string), "") as string;
            return "".Equals(path) ? defaultValue : path;
        }
    }
}