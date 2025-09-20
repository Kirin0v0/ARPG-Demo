using Features.Game.Data;
using Framework.Common.Debug;
using Framework.Common.UI.Panel;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace Features.Game.UI
{
    public class GameLoadingPanel : BaseUGUIPanel
    {
        [SerializeField] private char loadingCharacter = '.';
        [SerializeField] private int maxCharacterNumber = 3;
        [SerializeField] private float unitShowTime = 1f;
        
        private TextMeshProUGUI _textProgressName;
        private Slider _sliderProgressValue;

        [Inject] private IGameUIModel _gameUIModel;

        private string _loadingText;
        private int _characterNumber;
        private float _time;

        private string LoadingText => _loadingText + new string(loadingCharacter, _characterNumber);

        protected override void OnInit()
        {
            _textProgressName = GetWidget<TextMeshProUGUI>("TextProgressName");
            _sliderProgressValue = GetWidget<Slider>("SliderProgressValue");
        }

        protected override void OnShow(object payload)
        {
            _gameUIModel.GetLoadingData().ObserveForever(HandleLoadingDataChanged);
            ResetShowParameters();
            if (_gameUIModel.GetLoadingData().HasValue())
            {
                var loadingData = _gameUIModel.GetLoadingData().Value;
                _loadingText = loadingData.Name;
                _sliderProgressValue.value = loadingData.Progress;
            }
            else
            {
                _loadingText = "";
                _sliderProgressValue.value = 0;
            }
            _textProgressName.text = LoadingText;
        }

        protected override void OnShowingUpdate(bool focus)
        {
            _time += Time.unscaledDeltaTime;
            if (_time >= unitShowTime)
            {
                _characterNumber++;
                if (_characterNumber > maxCharacterNumber)
                {
                    _characterNumber = 0;
                }
                _textProgressName.text = LoadingText;
                _time = 0f;
            }
        }

        protected override void OnHide()
        {
            _gameUIModel.GetLoadingData().RemoveObserver(HandleLoadingDataChanged);
        }

        private void HandleLoadingDataChanged(GameLoadingUIData data)
        {
            ResetShowParameters();
            _loadingText = data.Name;
            _textProgressName.text = LoadingText;
            _sliderProgressValue.value = data.Progress;
        }

        private void ResetShowParameters()
        {
            _characterNumber = 0;
            _time = 0f;
        }
    }
}