using Features.Main.Archive;
using Features.SceneGoto;
using Framework.Common.Debug;
using Framework.Common.UI.Panel;
using Inputs;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using VContainer;
using PlayerInputManager = Inputs.PlayerInputManager;

namespace Features.Main
{
    public class MainHomepagePanel : BaseUGUIPanel
    {
        [SerializeField] private BaseSceneGotoSO clickNewGameGotoSO;

        [Inject] private EventSystem _eventSystem;
        [Inject] private PlayerInputManager _playerInputManager;
        [Inject] private UGUIPanelManager _panelManager;
        [Inject] private IObjectResolver _objectResolver;

        private Button _btnNewGame;
        private Button _btnContinueGame;
        private Button _btnSettings;
        private Button _btnExit;
        private HorizontalLayoutGroup _tipLayout;

        protected override void OnInit()
        {
            _btnNewGame = GetWidget<Button>("BtnNewGame");
            _btnContinueGame = GetWidget<Button>("BtnContinueGame");
            _btnSettings = GetWidget<Button>("BtnSettings");
            _btnExit = GetWidget<Button>("BtnExit");
            _tipLayout = GetWidget<HorizontalLayoutGroup>("TipLayout");
        }

        protected override void OnShow(object payload)
        {
            _btnNewGame.onClick.AddListener(OnClickButtonNewGame);
            _btnContinueGame.onClick.AddListener(OnClickButtonContinueGame);
            _btnSettings.onClick.AddListener(OnClickButtonSettings);
            _btnExit.onClick.AddListener(OnClickButtonExit);
        }

        protected override void OnShowingUpdate(bool focus)
        {
            if (focus && !_eventSystem.currentSelectedGameObject &&
                _playerInputManager.WasPerformedThisFrame(InputConstants.Navigate))
            {
                _eventSystem.SetSelectedGameObject(_btnNewGame.gameObject);
            }

            _tipLayout.gameObject.SetActive(focus);
        }

        protected override void OnHide()
        {
            _btnNewGame.onClick.RemoveListener(OnClickButtonNewGame);
            _btnContinueGame.onClick.RemoveListener(OnClickButtonContinueGame);
            _btnSettings.onClick.RemoveListener(OnClickButtonSettings);
            _btnExit.onClick.RemoveListener(OnClickButtonExit);
        }

        private void OnClickButtonNewGame()
        {
            clickNewGameGotoSO.Goto(null);
        }

        private void OnClickButtonContinueGame()
        {
            _panelManager.Show<MainArchivePanel>(
                UGUIPanelLayer.System,
                panel => { _objectResolver.Inject(panel); },
                null
            );
        }

        private void OnClickButtonSettings()
        {
            _panelManager.Show<MainSettingsPanel>(
                UGUIPanelLayer.System,
                panel => { _objectResolver.Inject(panel); },
                null
            );
        }

        private void OnClickButtonExit()
        {
            Application.Quit();
        }
    }
}