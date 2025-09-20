using System.Collections;
using Common;
using Features.Game.Data;
using Framework.Common.Audio;
using Framework.Common.UI.Panel;
using TMPro;
using UnityEngine;
using VContainer;

namespace Features.Game.UI
{
    public class GameCutscenePanel : BaseUGUIPanel
    {
        private TextMeshProUGUI _textTitle;

        [Inject] private AudioManager _audioManager;
        [Inject] private GameUIModel _gameUIModel;

        private int _audioId;
        private GameCutsceneUIData _data;

        protected override bool ShowingDebounce() => false;

        protected override void OnInit()
        {
            _textTitle = GetWidget<TextMeshProUGUI>("TextTitle");
        }

        protected override void OnShow(object payload)
        {
            _data = payload as GameCutsceneUIData;
            StopAllCoroutines();
            _textTitle.text = _data.Title ?? "";
            if (_data.Audio)
            {
                _audioId = _audioManager.PlaySound(_data.Audio);
            }

            StartCoroutine(HideDelay(_data.Duration));
        }

        protected override void OnShowingUpdate(bool focus)
        {
        }

        protected override void OnHide()
        {
            _audioManager.StopSound(_audioId);
            StopAllCoroutines();
        }

        private IEnumerator HideDelay(float time)
        {
            yield return new WaitForSecondsRealtime(time);
            _gameUIModel.CutsceneUI.SetValue(_gameUIModel.CutsceneUI.Value.Close());
            _data.OnFinished?.Invoke();
        }
    }
}