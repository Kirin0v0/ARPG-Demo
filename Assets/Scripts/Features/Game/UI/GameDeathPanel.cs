using Common;
using Damage;
using Damage.Data;
using Features.Game.Data;
using Features.SceneGoto;
using Framework.Common.UI.Panel;
using Framework.Common.Util;
using Inputs;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using VContainer;

namespace Features.Game.UI
{
    public class GameDeathPanel : BaseUGUIPanel
    {
        [SerializeField] private BaseSceneGotoSO mainSceneGotoSO;

        [Inject] private GameManager _gameManager;
        [Inject] private DamageManager _damageManager;
        [Inject] private EventSystem _eventSystem;
        [Inject] private PlayerInputManager _playerInputManager;

        private TextMeshProUGUI _textMessage;
        private Button _btnRespawn;
        private Button _btnLoadRecentArchive;
        private Button _btnBackToHomepage;
        private HorizontalLayoutGroup _tipLayout;

        protected override void OnInit()
        {
            _textMessage = GetWidget<TextMeshProUGUI>("TextMessage");
            _btnRespawn = GetWidget<Button>("BtnRespawn");
            _btnLoadRecentArchive = GetWidget<Button>("BtnLoadRecentArchive");
            _btnBackToHomepage = GetWidget<Button>("BtnBackToHomepage");
            _tipLayout = GetWidget<HorizontalLayoutGroup>("TipLayout");
        }

        protected override void OnShow(object payload)
        {
            var data = payload as GameDeathUIData;

            _btnRespawn.onClick.AddListener(OnRespawnButtonClicked);
            _btnLoadRecentArchive.onClick.AddListener(OnLoadRecentArchiveButtonClicked);
            _btnBackToHomepage.onClick.AddListener(OnBackToHomepageButtonClicked);

            _textMessage.text = data!.Message;

            if (Focus)
            {
                _eventSystem.SetSelectedGameObject(null);
            }
        }

        protected override void OnShowingUpdate(bool focus)
        {
            if (focus && !_eventSystem.currentSelectedGameObject &&
                _playerInputManager.WasPerformedThisFrame(InputConstants.Navigate))
            {
                _eventSystem.SetSelectedGameObject(_btnRespawn.gameObject);
            }

            _tipLayout.gameObject.SetActive(focus);
        }

        protected override void OnHide()
        {
            _btnRespawn.onClick.RemoveListener(OnRespawnButtonClicked);
            _btnLoadRecentArchive.onClick.RemoveListener(OnLoadRecentArchiveButtonClicked);
            _btnBackToHomepage.onClick.RemoveListener(OnBackToHomepageButtonClicked);

            UGUIUtil.DeselectIfSelectedInTargetChildren(_eventSystem, transform);
        }

        private void OnRespawnButtonClicked()
        {
            if (!_gameManager.Player)
            {
                return;
            }

            _damageManager.AddDamage(
                _gameManager.God,
                _gameManager.Player,
                DamageEnvironmentMethod.Default,
                DamageType.DirectHeal,
                new DamageValue
                {
                    noType = -_gameManager.Player.Parameters.property.maxHp,
                },
                DamageResourceMultiplier.Hp,
                0f,
                _gameManager.Player.transform.forward,
                true
            );
            _damageManager.AddDamage(
                _gameManager.God,
                _gameManager.Player,
                DamageEnvironmentMethod.Default,
                DamageType.DirectHeal,
                new DamageValue
                {
                    noType = -_gameManager.Player.Parameters.property.maxMp,
                },
                DamageResourceMultiplier.Hp,
                0f,
                _gameManager.Player.transform.forward,
                true
            );
            _gameManager.Player.transform.position = _gameManager.Player.PlayerParameters.lastSafePosition;
            Physics.SyncTransforms();
        }

        private void OnLoadRecentArchiveButtonClicked()
        {
            GameApplication.Instance.ArchiveManager.NotifyNewestLoad();
        }

        private void OnBackToHomepageButtonClicked()
        {
            mainSceneGotoSO?.Goto(null);
        }
    }
}