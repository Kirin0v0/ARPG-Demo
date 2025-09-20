using System.Globalization;
using Common;
using Framework.Common.Debug;
using Framework.Common.UI.Panel;
using Framework.Common.Util;
using Inputs;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using VContainer;
using PlayerInputManager = Inputs.PlayerInputManager;

namespace Features.Main
{
    public class MainSettingsPanel : BaseUGUIPanel
    {
        [Inject] private EventSystem _eventSystem;
        [Inject] private PlayerInputManager _playerInputManager;
        [Inject] private UGUIPanelManager _panelManager;
        [Inject] private IObjectResolver _objectResolver;

        private Button _btnClose;
        private TMP_Dropdown _ddScreenMode;
        private TMP_Dropdown _ddResolution;
        private TMP_Dropdown _ddFrameRate;
        private Slider _sliderMusic;
        private TextMeshProUGUI _textMusicValue;
        private Slider _sliderSound;
        private TextMeshProUGUI _textSoundValue;

        protected override void OnInit()
        {
            _btnClose = GetWidget<Button>("BtnClose");
            _ddScreenMode = GetWidget<TMP_Dropdown>("DdScreenMode");
            _ddResolution = GetWidget<TMP_Dropdown>("DdResolution");
            _ddFrameRate = GetWidget<TMP_Dropdown>("DdFrameRate");
            _sliderMusic = GetWidget<Slider>("SliderMusic");
            _textMusicValue = GetWidget<TextMeshProUGUI>("TextMusicValue");
            _sliderSound = GetWidget<Slider>("SliderSound");
            _textSoundValue = GetWidget<TextMeshProUGUI>("TextSoundValue");
        }

        protected override void OnShow(object payload)
        {
            _btnClose.onClick.AddListener(OnCloseButtonClicked);
            _ddScreenMode.onValueChanged.AddListener(OnScreenModeDropdownChanged);
            _ddResolution.onValueChanged.AddListener(OnResolutionDropdownChanged);
            _ddFrameRate.onValueChanged.AddListener(OnFrameRateDropdownChanged);
            _sliderMusic.onValueChanged.AddListener(OnMusicSliderChanged);
            _sliderSound.onValueChanged.AddListener(OnSoundSliderChanged);

            if (Focus)
            {
                _eventSystem.SetSelectedGameObject(null);
            }
            _ddScreenMode.value = (int)GameApplication.Instance.GlobalSettingsDataManager.ScreenMode;
            _ddResolution.value = (int)GameApplication.Instance.GlobalSettingsDataManager.DisplayResolution;
            _ddFrameRate.value = (int)GameApplication.Instance.GlobalSettingsDataManager.FrameRate;
            _sliderMusic.value = GameApplication.Instance.GlobalSettingsDataManager.MusicVolume;
            _sliderSound.value = GameApplication.Instance.GlobalSettingsDataManager.SoundVolume;
        }

        protected override void OnShowingUpdate(bool focus)
        {
            if (focus && !_eventSystem.currentSelectedGameObject &&
                _playerInputManager.WasPerformedThisFrame(InputConstants.Navigate))
            {
                _eventSystem.SetSelectedGameObject(_btnClose.gameObject);
            }

            if (focus && _playerInputManager.WasPerformedThisFrame(InputConstants.Cancel))
            {
                _eventSystem.SetSelectedGameObject(_btnClose.gameObject);
            }
        }

        protected override void OnHide()
        {
            _btnClose.onClick.RemoveListener(OnCloseButtonClicked);
            _ddScreenMode.onValueChanged.RemoveListener(OnScreenModeDropdownChanged);
            _ddResolution.onValueChanged.RemoveListener(OnResolutionDropdownChanged);
            _ddFrameRate.onValueChanged.RemoveListener(OnFrameRateDropdownChanged);
            _sliderMusic.onValueChanged.RemoveListener(OnMusicSliderChanged);
            _sliderSound.onValueChanged.RemoveListener(OnSoundSliderChanged);

            UGUIUtil.DeselectIfSelectedInTargetChildren(_eventSystem, transform);
        }

        private void OnCloseButtonClicked()
        {
            _panelManager.Hide<MainSettingsPanel>();
        }

        private void OnScreenModeDropdownChanged(int index)
        {
            GameApplication.Instance.GlobalSettingsDataManager.ScreenMode = index switch
            {
                1 => Common.ScreenMode.FullScreenWindow,
                2 => ScreenMode.Windowed,
                _ => Common.ScreenMode.ExclusiveFullScreen
            };
        }

        private void OnResolutionDropdownChanged(int index)
        {
            GameApplication.Instance.GlobalSettingsDataManager.DisplayResolution = index switch
            {
                1 => Common.DisplayResolution.W1280H960,
                2 => Common.DisplayResolution.W1440H1080,
                3 => Common.DisplayResolution.W2560H1440,
                _ => Common.DisplayResolution.W1920H1080
            };
        }

        private void OnFrameRateDropdownChanged(int index)
        {
            GameApplication.Instance.GlobalSettingsDataManager.FrameRate = index switch
            {
                1 => Common.FrameRate.FPS30,
                2 => Common.FrameRate.FPS120,
                _ => Common.FrameRate.FPS60
            };
        }

        private void OnMusicSliderChanged(float value)
        {
            GameApplication.Instance.GlobalSettingsDataManager.MusicVolume = value;
            _textMusicValue.text = value.ToString(CultureInfo.InvariantCulture);
        }

        private void OnSoundSliderChanged(float value)
        {
            GameApplication.Instance.GlobalSettingsDataManager.SoundVolume = value;
            _textSoundValue.text = value.ToString(CultureInfo.InvariantCulture);
        }
    }
}