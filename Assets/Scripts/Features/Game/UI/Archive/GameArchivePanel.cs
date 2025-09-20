using System.Collections.Generic;
using Features.Game.Data;
using Features.SceneGoto;
using Framework.Common.UI.Panel;
using Framework.Common.UI.RecyclerView;
using Framework.Common.UI.RecyclerView.LayoutManager;
using Framework.Common.Util;
using Inputs;
using Map.Data;
using Quest;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using VContainer;
using PlayerInputManager = Inputs.PlayerInputManager;

namespace Features.Game.UI.Archive
{
    public class GameArchivePanel : BaseUGUIPanel
    {
        private Button _btnClose;
        private TextMeshProUGUI _textNavigate;
        private TextMeshProUGUI _textSubmit;
        private TextMeshProUGUI _textLoad;
        private TextMeshProUGUI _textDelete;
        private RecyclerView _rvArchiveSlotList;
        [SerializeField] private RecyclerViewSelectable recyclerViewSelectable;
        [SerializeField] private GameArchiveListAdapter adapter;
        [SerializeField] private RecyclerViewLayoutManager layoutManager;
        [SerializeField] private int archiveSlotCount = 10;

        [Inject] private UGUIPanelManager _panelManager;
        [Inject] private QuestManager _questManager;
        [Inject] private EventSystem _eventSystem;
        [Inject] private PlayerInputManager _playerInputManager;
        [Inject] private GameUIModel _gameUIModel;

        private GameArchiveModel _archiveModel;

        protected override void OnInit()
        {
            _btnClose = GetWidget<Button>("BtnClose");
            _textNavigate = GetWidget<TextMeshProUGUI>("TextNavigate");
            _textSubmit = GetWidget<TextMeshProUGUI>("TextSubmit");
            _textLoad = GetWidget<TextMeshProUGUI>("TextLoad");
            _textDelete = GetWidget<TextMeshProUGUI>("TextDelete");
            _rvArchiveSlotList = GetWidget<RecyclerView>("RvArchiveSlotList");

            // 初始化RecyclerView
            _rvArchiveSlotList.Init();
            _rvArchiveSlotList.LayoutManager = layoutManager;
            adapter.Init(
                HandleNavigationMove,
                HandleArchiveItemClicked,
                HandleArchiveItemDeleted
            );
            _rvArchiveSlotList.Adapter = adapter;
        }

        protected override void OnShow(object payload)
        {
            _btnClose.onClick.AddListener(HandleCloseButtonClicked);
            _playerInputManager.RegisterActionPerformed(InputConstants.Delete, HandleDeleteConfirmed);

            _archiveModel = new GameArchiveModel(GameApplication.Instance.ArchiveManager,
                GameApplication.Instance.ExcelBinaryManager.GetContainer<MapInfoContainer>(), _questManager.QuestPool);
            _archiveModel.GetArchiveSlotList().ObserveForever(HandleArchiveSlotListChanged);
            _archiveModel.FetchArchiveSlotList(archiveSlotCount);

            _rvArchiveSlotList.ScrollToPosition(0);
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

            if (!focus)
            {
                _textNavigate.gameObject.SetActive(false);
                _textSubmit.gameObject.SetActive(false);
                _textLoad.gameObject.SetActive(false);
                _textDelete.gameObject.SetActive(false);
            }
            else
            {
                _textNavigate.gameObject.SetActive(true);
                if (_eventSystem.currentSelectedGameObject)
                {
                    var archiveItemViewHolder =
                        _eventSystem.currentSelectedGameObject.GetComponent<GameArchiveItemViewHolder>();
                    if (archiveItemViewHolder)
                    {
                        _textSubmit.gameObject.SetActive(false);
                        _textLoad.gameObject.SetActive(true);
                        _textDelete.gameObject.SetActive(true);
                    }
                    else
                    {
                        _textSubmit.gameObject.SetActive(true);
                        _textLoad.gameObject.SetActive(false);
                        _textDelete.gameObject.SetActive(false);
                    }
                }
                else
                {
                    _textSubmit.gameObject.SetActive(false);
                    _textLoad.gameObject.SetActive(false);
                    _textDelete.gameObject.SetActive(false);
                }
            }
        }

        protected override void OnHide()
        {
            _archiveModel.GetArchiveSlotList().RemoveObserver(HandleArchiveSlotListChanged);
            _archiveModel = null;

            _btnClose.onClick.RemoveListener(HandleCloseButtonClicked);
            _playerInputManager.UnregisterActionPerformed(InputConstants.Delete, HandleDeleteConfirmed);

            UGUIUtil.DeselectIfSelectedInTargetChildren(_eventSystem, transform);
        }

        private void HandleCloseButtonClicked()
        {
            _gameUIModel.ArchiveUI.SetValue(_gameUIModel.ArchiveUI.Value.Close());
        }

        private void HandleDeleteConfirmed(InputAction.CallbackContext obj)
        {
            if (!Focus || !_eventSystem.currentSelectedGameObject) return;
            var itemViewHolder = _eventSystem.currentSelectedGameObject.GetComponent<GameArchiveItemViewHolder>();
            itemViewHolder?.DeleteItem();
        }

        private void HandleArchiveSlotListChanged(List<object> archiveSlots)
        {
            adapter.SetData(archiveSlots);
        }

        private void HandleNavigationMove(RecyclerViewHolder viewHolder, MoveDirection moveDirection)
        {
            switch (moveDirection)
            {
                case MoveDirection.Up:
                {
                    if (_archiveModel.SelectPreviousItem(viewHolder.Position, out int index, out object data))
                    {
                        adapter.RefreshItem(index, data);
                        _rvArchiveSlotList.FocusItem(index, true);
                    }
                    else
                    {
                        _eventSystem.SetSelectedGameObject(recyclerViewSelectable.navigation.selectOnUp
                            ?.gameObject);
                    }
                }
                    break;
                case MoveDirection.Down:
                {
                    if (_archiveModel.SelectNextItem(viewHolder.Position, out int index, out object data))
                    {
                        adapter.RefreshItem(index, data);
                        _rvArchiveSlotList.FocusItem(index, true);
                    }
                }
                    break;
            }
        }

        private void HandleArchiveItemClicked(GameArchiveItemUIData data)
        {
            GameApplication.Instance.ArchiveManager.NotifyLoad(data.Id);
        }

        private void HandleArchiveItemDeleted(GameArchiveItemUIData data)
        {
            GameApplication.Instance.ArchiveManager.DeleteArchive(data.Id);
            _archiveModel.FetchArchiveSlotList(archiveSlotCount);
        }
    }
}