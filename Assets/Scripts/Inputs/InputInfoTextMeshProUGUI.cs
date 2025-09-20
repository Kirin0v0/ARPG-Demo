using System;
using Framework.Common.Debug;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

namespace Inputs
{
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class InputInfoTextMeshProUGUI : MonoBehaviour
    {
        private InputInfoManager _inputInfoManager;

        [Title("设备文字配置")] [InlineButton("ShowKeyboardMouseText", "展示键鼠文字")] [SerializeField, TextArea]
        private string keyboardMouseText;

        [InlineButton("ShowPsText", "展示PS文字")] [SerializeField, TextArea]
        private string psText;

        [InlineButton("ShowXboxText", "展示Xbox文字")] [SerializeField, TextArea]
        private string xboxText;

        [InlineButton("ShowNsText", "展示NS文字")] [SerializeField, TextArea]
        private string nsText;

        private TextMeshProUGUI _textMeshProUGUI;

        public TextMeshProUGUI TextMeshProUGUI
        {
            get
            {
                if (!_textMeshProUGUI)
                {
                    _textMeshProUGUI = GetComponent<TextMeshProUGUI>();
                }

                return _textMeshProUGUI;
            }
        }

        private void Awake()
        {
            var inputInfoManager = GameEnvironment.FindEnvironmentComponent<InputInfoManager>();
            if (inputInfoManager)
            {
                Init(inputInfoManager);
            }
        }

        public void Init(InputInfoManager inputInfoManager)
        {
            if (_inputInfoManager)
            {
                _inputInfoManager.OnInputDeviceChanged -= SetInputDeviceText;
            }

            _inputInfoManager = inputInfoManager;
            if (_inputInfoManager)
            {
                SetInputDeviceText(_inputInfoManager.InputDeviceType);
                _inputInfoManager.OnInputDeviceChanged += SetInputDeviceText;
            }
        }

        private void OnDestroy()
        {
            if (_inputInfoManager)
            {
                _inputInfoManager.OnInputDeviceChanged -= SetInputDeviceText;
            }
        }

        private void SetInputDeviceText(InputDeviceType deviceType)
        {
            switch (deviceType)
            {
                case InputDeviceType.KeyboardAndMouse:
                    ShowKeyboardMouseText();
                    break;
                case InputDeviceType.DualShockGamepad:
                    ShowPsText();
                    break;
                case InputDeviceType.XboxGamepad:
                    ShowXboxText();
                    break;
                case InputDeviceType.NintendoSwitchGamepad:
                    ShowNsText();
                    break;
            }
        }

        private void ShowKeyboardMouseText()
        {
            TextMeshProUGUI.text = keyboardMouseText;
        }

        private void ShowPsText()
        {
            TextMeshProUGUI.text = psText;
        }

        private void ShowXboxText()
        {
            TextMeshProUGUI.text = xboxText;
        }

        private void ShowNsText()
        {
            TextMeshProUGUI.text = nsText;
        }
    }
}