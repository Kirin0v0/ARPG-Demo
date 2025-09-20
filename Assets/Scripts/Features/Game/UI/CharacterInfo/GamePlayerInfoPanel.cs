using Character;
using DG.Tweening;
using Framework.Common.Debug;
using Framework.Common.UI.Panel;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Features.Game.UI.CharacterInfo
{
    public class GamePlayerInfoPanel : BaseUGUIPanel
    {
        private TextMeshProUGUI _textTitle;

        private CharacterObject _player;

        protected override void OnInit()
        {
            _textTitle = GetWidget<TextMeshProUGUI>("TextTitle");
        }

        protected override void OnShow(object payload)
        {
        }

        protected override void OnShowingUpdate(bool focus)
        {
            if (!_player)
            {
                return;
            }

            UpdateCharacterInfo(true);
        }

        protected override void OnHide()
        {
        }

        public void BindPlayer(CharacterObject player)
        {
            _player = player;
            UpdateCharacterInfo(false);
        }

        public void UnbindPlayer()
        {
            _player = null;
        }

        private void UpdateCharacterInfo(bool tickUpdate)
        {
            _textTitle.text = _player.Parameters.name;
        }
    }
}