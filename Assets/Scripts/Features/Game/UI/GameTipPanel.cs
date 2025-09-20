using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Events;
using Events.Data;
using Features.Game.Data;
using Framework.Common.UI.Panel;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace Features.Game.UI
{
    public class GameTipPanel : BaseUGUIPanel
    {
        [SerializeField] private float showTime = 1f;
        [SerializeField] private float continueTime = 2f;
        [SerializeField] private float dismissTime = 1f;

        [Inject] private GameUIModel _gameUIModel;

        private readonly List<string> _tips = new();

        private Image _tipLayout;
        private CanvasGroup _canvasGroup;
        private TextMeshProUGUI _textTip;
        private Sequence _sequence;

        protected override bool ShowingDebounce() => false;

        protected override void OnInit()
        {
            _tipLayout = GetWidget<Image>("TipLayout");
            _canvasGroup = _tipLayout.GetComponent<CanvasGroup>();
            _textTip = GetWidget<TextMeshProUGUI>("TextTip");
        }

        protected override void OnShow(object payload)
        {
            _canvasGroup.alpha = 0f;
            // 监听提示事件
            GameApplication.Instance.EventCenter.AddEventListener<TipEventParameter>(GameEvents.Tip, HandleTipEvent);
        }

        protected override void OnShowingUpdate(bool focus)
        {
            // 每帧都去尝试显示提示
            TryToShowTip();
        }

        protected override void OnHide()
        {
            // 清空提示列表和动画
            _canvasGroup.alpha = 0f;
            _tips.Clear();
            if (_sequence != null)
            {
                _sequence.Kill();
                _sequence = null;
            }

            // 取消监听提示事件
            GameApplication.Instance?.EventCenter.RemoveEventListener<TipEventParameter>(GameEvents.Tip, HandleTipEvent);
        }

        private void HandleTipEvent(TipEventParameter parameter)
        {
            _tips.Add(parameter.Tip);
            TryToShowTip();
        }

        private void TryToShowTip()
        {
            if (_sequence == null && _tips.Count > 0)
            {
                ShowTip(_tips[0]);
                _tips.RemoveAt(0);
            }
        }

        private void ShowTip(string tip)
        {
            _tipLayout.transform.localScale = Vector3.zero;
            _canvasGroup.alpha = 1f;
            _textTip.text = tip;
            _sequence = DOTween.Sequence();
            _sequence.Append(DOTween.To(() => _tipLayout.transform.localScale, x => _tipLayout.transform.localScale = x, Vector3.one,
                showTime));
            _sequence.AppendInterval(continueTime);
            _sequence.Append(DOTween.To(() => _canvasGroup.alpha, x => _canvasGroup.alpha = x, 0, dismissTime));
            _sequence.onComplete = () =>
            {
                _sequence = null;
            };
        }
    }
}