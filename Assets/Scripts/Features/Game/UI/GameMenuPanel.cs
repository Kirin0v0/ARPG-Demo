using Character;
using Common;
using Features.SceneGoto;
using Framework.Common.Debug;
using Framework.Common.UI.Panel;
using Framework.Common.UI.Toast;
using Framework.Common.Util;
using Inputs;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.UIElements;
using VContainer;
using Button = UnityEngine.UI.Button;
using PlayerInputManager = Inputs.PlayerInputManager;

namespace Features.Game.UI
{
    public class GameMenuPanel : BaseUGUIPanel
    {
        [Inject] private PlayerInputManager _playerInputManager;
        [Inject] private GameUIModel _gameUIModel;
        [Inject] private GameManager _gameManager;
        [Inject] private EventSystem _eventSystem;

        [SerializeField] private BaseSceneGotoSO backSceneGotoSO;

        private ScrollRect _scrollView;
        private Button _btnClose;
        private Button _btnCharacter;
        private Button _btnPackage;
        private Button _btnQuest;
        private Button _btnSave;
        private Button _btnLoadNewestArchive;
        private Button _btnLoad;
        private Button _btnBackToMain;

        protected override void OnInit()
        {
            _scrollView = GetWidget<ScrollRect>("ScrollView");
            _btnClose = GetWidget<Button>("BtnClose");
            _btnCharacter = GetWidget<Button>("BtnCharacter");
            _btnPackage = GetWidget<Button>("BtnPackage");
            _btnQuest = GetWidget<Button>("BtnQuest");
            _btnSave = GetWidget<Button>("BtnSave");
            _btnLoadNewestArchive = GetWidget<Button>("BtnLoadNewestArchive");
            _btnLoad = GetWidget<Button>("BtnLoad");
            _btnBackToMain = GetWidget<Button>("BtnBackToMain");
        }

        protected override void OnShow(object payload)
        {
            _btnClose.onClick.AddListener(OnButtonCloseClicked);
            _btnCharacter.onClick.AddListener(OnButtonCharacterClicked);
            _btnPackage.onClick.AddListener(OnButtonPackageClicked);
            _btnQuest.onClick.AddListener(OnButtonQuestClicked);
            _btnSave.onClick.AddListener(OnButtonSaveClicked);
            _btnLoadNewestArchive.onClick.AddListener(OnButtonLoadNewestArchiveClicked);
            _btnLoad.onClick.AddListener(OnButtonLoadClicked);
            _btnBackToMain.onClick.AddListener(OnButtonBackToMainClicked);

            _scrollView.verticalNormalizedPosition = 1f;
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
                _eventSystem.SetSelectedGameObject(_btnClose.gameObject);
            }
            
            if (focus && _playerInputManager.WasPerformedThisFrame(InputConstants.Cancel))
            {
                _eventSystem.SetSelectedGameObject(_btnClose.gameObject);
            }
        }

        protected override void OnHide()
        {
            _btnClose.onClick.RemoveListener(OnButtonCloseClicked);
            _btnCharacter.onClick.RemoveListener(OnButtonCharacterClicked);
            _btnPackage.onClick.RemoveListener(OnButtonPackageClicked);
            _btnQuest.onClick.RemoveListener(OnButtonQuestClicked);
            _btnSave.onClick.RemoveListener(OnButtonSaveClicked);
            _btnLoadNewestArchive.onClick.RemoveListener(OnButtonLoadNewestArchiveClicked);
            _btnLoad.onClick.RemoveListener(OnButtonLoadClicked);
            _btnBackToMain.onClick.RemoveListener(OnButtonBackToMainClicked);
            UGUIUtil.DeselectIfSelectedInTargetChildren(_eventSystem, transform);
        }

        private void OnButtonCloseClicked()
        {
            _gameUIModel.MenuUI.SetValue(_gameUIModel.MenuUI.Value.Close());
        }

        private void OnButtonCharacterClicked()
        {
            DebugUtil.LogYellow("OnButtonCharacterClicked");
            _gameUIModel.CharacterUI.SetValue(_gameUIModel.CharacterUI.Value.Open());
        }

        private void OnButtonPackageClicked()
        {
            _gameUIModel.PackageUI.SetValue(_gameUIModel.PackageUI.Value.Open());
        }

        private void OnButtonQuestClicked()
        {
            _gameUIModel.QuestUI.SetValue(_gameUIModel.QuestUI.Value.Open());
        }

        private void OnButtonSaveClicked()
        {
            if (!_gameManager.Player)
            {
                Toast.Instance.Show("无法保存游戏");
                return;
            }

            if (_gameManager.Player.Parameters.dead)
            {
                Toast.Instance.Show("玩家死亡，无法保存");
                return;
            }

            if (_gameManager.Player.Parameters.battleState == CharacterBattleState.Battle)
            {
                Toast.Instance.Show("玩家处于战斗中，无法保存");
                return;
            }

            GameApplication.Instance.ArchiveManager.NotifySave(false);
            Toast.Instance.Show("保存成功");
        }

        private void OnButtonLoadNewestArchiveClicked()
        {
            GameApplication.Instance.ArchiveManager.NotifyNewestLoad();
        }

        private void OnButtonLoadClicked()
        {
            _gameUIModel.ArchiveUI.SetValue(_gameUIModel.ArchiveUI.Value.Open());
        }

        private void OnButtonBackToMainClicked()
        {
            backSceneGotoSO?.Goto(null);
        }
    }
}