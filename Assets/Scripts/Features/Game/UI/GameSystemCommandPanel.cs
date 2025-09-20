using System;
using Common;
using Framework.Common.Debug;
using Framework.Common.UI.Panel;
using Inputs;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using VContainer;
using PlayerInputManager = Inputs.PlayerInputManager;

namespace Features.Game.UI
{
    public class GameSystemCommandPanel : BaseUGUIPanel
    {
        [Inject] private PlayerInputManager _playerInputManager;
        [Inject] private GameUIModel _gameUIModel;

        protected override void OnInit()
        {
        }

        protected override void OnShow(object payload)
        {
            // 监听玩家输入
            _playerInputManager.RegisterActionPerformed(InputConstants.Menu, HandleMenuPerformed);
            _playerInputManager.RegisterActionPerformed(InputConstants.Map, HandleMapPerformed);
        }

        protected override void OnShowingUpdate(bool focus)
        {
        }

        protected override void OnHide()
        {
            // 取消监听玩家输入
            _playerInputManager.UnregisterActionPerformed(InputConstants.Menu, HandleMenuPerformed);
            _playerInputManager.UnregisterActionPerformed(InputConstants.Map, HandleMapPerformed);
        }

        private void HandleMenuPerformed(InputAction.CallbackContext obj)
        {
            if (!Focus)
            {
                return;
            }

            _gameUIModel.MenuUI.SetValue(_gameUIModel.MenuUI.Value.Open());
        }

        private void HandleMapPerformed(InputAction.CallbackContext obj)
        {
            if (!Focus)
            {
                return;
            }

            _gameUIModel.MapUI.SetValue(_gameUIModel.MapUI.Value.Open());
        }
    }
}