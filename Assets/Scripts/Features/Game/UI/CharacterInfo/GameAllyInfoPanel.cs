using System;
using System.Collections.Generic;
using Character;
using DG.Tweening;
using Framework.Common.Debug;
using Framework.Common.UI.Panel;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Features.Game.UI.CharacterInfo
{
    public class GameAllyInfoPanel : BaseUGUIPanel
    {
        [Serializable]
        private class AllyInfoConfigurationData
        {
            public string tag;
            public Sprite background;
            public Color textColor;
        }

        [SerializeField] private Sprite defaultBackground;
        [SerializeField] private Color defaultTextColor;
        [SerializeField] private List<AllyInfoConfigurationData> extraConfigurations = new();

        private Image _imgTitleBar;
        private TextMeshProUGUI _textTitle;

        private CharacterObject _ally;

        protected override void OnInit()
        {
            _imgTitleBar = GetWidget<Image>("ImgTitleBar");
            _textTitle = GetWidget<TextMeshProUGUI>("TextTitle");
        }

        protected override void OnShow(object payload)
        {
        }

        protected override void OnShowingUpdate(bool focus)
        {
            if (!_ally)
            {
                return;
            }

            UpdateCharacterInfo(true);
        }

        protected override void OnHide()
        {
        }

        public void BindAlly(CharacterObject ally)
        {
            _ally = ally;
            UpdateCharacterInfo(false);
        }

        public void UnbindAlly()
        {
            _ally = null;
        }

        private void UpdateCharacterInfo(bool tickUpdate)
        {
            var information = GlobalRuleSingletonConfigSO.Instance.allyInformation.Find(x => _ally.HasTag(x.tag));
            if (information == null)
            {
                _textTitle.text = _ally.Parameters.name;
                _imgTitleBar.color = GlobalRuleSingletonConfigSO.Instance.allyDefaultColor;
            }
            else
            {
                _textTitle.text = information.prefixTitle + _ally.Parameters.name;
                _imgTitleBar.color = information.color;
            }

            // 查找额外配置
            var extraConfiguration = extraConfigurations.Find(x => _ally.HasTag(x.tag));
            if (extraConfiguration == null)
            {
                _imgTitleBar.sprite = defaultBackground;
                _textTitle.color = defaultTextColor;
            }
            else
            {
                _imgTitleBar.sprite = extraConfiguration.background;
                _textTitle.color = extraConfiguration.textColor;
            }
        }
    }
}