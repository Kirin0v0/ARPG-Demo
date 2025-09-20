using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Camera;
using Camera.Data;
using Character;
using Common;
using Events;
using Features.Game.Data;
using Framework.Common.Debug;
using Framework.Common.UI.Panel;
using Framework.Common.UI.RecyclerView;
using Framework.Common.UI.RecyclerView.Adapter;
using Framework.Common.UI.RecyclerView.LayoutManager;
using Framework.Common.UI.Toast;
using Framework.Common.Util;
using Inputs;
using Player;
using Sirenix.OdinInspector;
using Skill;
using Skill.Unit;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using VContainer;
using PlayerInputManager = Inputs.PlayerInputManager;

namespace Features.Game.UI.BattleCommand
{
    public class GameBattleCommandPanel : BaseUGUIPanel
    {
        [Inject] private GameManager _gameManager;
        [Inject] private GameUIModel _gameUIModel;
        [Inject] private PlayerInputManager _playerInputManager;
        [Inject] private ICameraModel _cameraModel;
        [Inject] private SkillManager _skillManager;
        [Inject] private IObjectResolver _objectResolver;
        [Inject] private EventSystem _eventSystem;

        private PlayerCharacterObject Player => _gameManager.Player;

        [Title("列表配置")] [SerializeField] private GameBattleCommandListAdapter adapter;
        [SerializeField] private RecyclerViewLayoutManager layoutManager;

        [Title("弹窗配置")] [SerializeField] private GameBattleCommandDetailPopup detailPopup;

        private VerticalLayoutGroup _collapseLayout;
        private RectTransform _expandLayout;
        private TextMeshProUGUI _textBattleCommandTitle;
        private RecyclerView _rvBattleCommandList;
        private GameBattleCommandModel _battleCommandModel;
        private HorizontalLayoutGroup _expandTipLayout;
        private TextMeshProUGUI _textDetailSwitch;
        private HorizontalLayoutGroup _skillTipLayout;
        private Image _imgBlocker;

        private bool _inTargetSelection = false;

        private bool InTargetSelection
        {
            get => _inTargetSelection;
            set
            {
                _inTargetSelection = value;
                if (value)
                {
                    _imgBlocker.gameObject.SetActive(true);
                    _expandTipLayout.gameObject.SetActive(false);
                    _skillTipLayout.gameObject.SetActive(true);
                }
                else
                {
                    _imgBlocker.gameObject.SetActive(false);
                    _expandTipLayout.gameObject.SetActive(true);
                    _skillTipLayout.gameObject.SetActive(false);
                }
            }
        }

        private bool _showDetail = false;

        protected override void OnInit()
        {
            _collapseLayout = GetWidget<VerticalLayoutGroup>("CollapseLayout");
            _expandLayout = GetWidget<RectTransform>("ExpandLayout");
            _textBattleCommandTitle = GetWidget<TextMeshProUGUI>("TextBattleCommandTitle");
            _rvBattleCommandList = GetWidget<RecyclerView>("RvBattleCommandList");
            _expandTipLayout = GetWidget<HorizontalLayoutGroup>("ExpandTipLayout");
            _textDetailSwitch = GetWidget<TextMeshProUGUI>("TextDetailSwitch");
            _skillTipLayout = GetWidget<HorizontalLayoutGroup>("SkillTipLayout");
            _imgBlocker = GetWidget<Image>("ImgBlocker");

            // 初始化RecyclerView
            _rvBattleCommandList.Init();
            _rvBattleCommandList.LayoutManager = layoutManager;
            _rvBattleCommandList.Adapter = adapter;

            // 设置弹窗行为
            detailPopup.AnchoredPositionGetter = rectTransform =>
            {
                var corners = new Vector3[4];
                rectTransform.GetWorldCorners(corners);
                return new Vector2(corners[2].x, (corners[2].y + corners[3].y) / 2);
            };
            detailPopup.PopupPositionSetter = (anchoredPosition, popup) =>
            {
                var popupSize = popup.sizeDelta;
                popup.pivot = anchoredPosition.y >= popupSize.y / 2f ? new Vector2(0, 0.5f) : new Vector2(0, 0f);
                popup.transform.position = anchoredPosition;
            };
        }

        protected override void OnShow(object payload)
        {
            adapter.Init(
                _gameManager.Player,
                HandleNavigationMove,
                HandleBattleCommandClicked
            );
            InTargetSelection = false;
            _showDetail = false;

            // 初始化Model
            _battleCommandModel = new GameBattleCommandModel(_gameManager, _skillManager);
            _battleCommandModel.GetPageData().ObserveForever(HandlePageDataChanged);
            _battleCommandModel.ShowCollapsePage();

            // 监听玩家输入
            _playerInputManager.RegisterActionPerformed(InputConstants.Command, HandleCommandPerformed);
            _playerInputManager.RegisterActionPerformed(InputConstants.Cancel, HandleCancelPerformed);
            _playerInputManager.RegisterActionPerformed(InputConstants.Detail, HandleDetailPerformed);

            // 监听选择事件
            GameApplication.Instance.EventCenter.AddEventListener<CharacterObject>(
                GameEvents.FinishTargetSelection, OnFinishTargetSelection);

            // 监听相机场景变化
            _cameraModel.GetScene().ObserveForever(OnCameraSceneChanged);

            _rvBattleCommandList.ScrollToPosition(0);
            if (Focus)
            {
                _eventSystem.SetSelectedGameObject(null);
            }
        }

        protected override void OnShowingUpdate(bool focus)
        {
            // 更新详情弹窗
            UpdateDetailPopup();

            // 设置页面显隐
            var pageVisible = Player && !Player.Parameters.dead;
            if (pageVisible)
            {
                _battleCommandModel.VisiblePage();
            }
            else
            {
                _battleCommandModel.InvisiblePage();
            }

            // 如果未选中页面元素则在导航时设置默认选中
            if (focus && _battleCommandModel.GetPageData().Value is GameBattleCommandExpandPageData &&
                !_eventSystem.currentSelectedGameObject &&
                _playerInputManager.WasPerformedThisFrame(InputConstants.Navigate))
            {
                _eventSystem.SetSelectedGameObject(SelectViewHolder());
            }
        }

        protected override void OnHide()
        {
            // 隐藏页面时同步状态
            _gameUIModel.BattleCommandExpanding.SetValue(false);

            // 销毁Model
            _battleCommandModel.GetPageData().RemoveObserver(HandlePageDataChanged);
            _battleCommandModel = null;

            // 取消监听玩家输入
            _playerInputManager.UnregisterActionPerformed(InputConstants.Command, HandleCommandPerformed);
            _playerInputManager.UnregisterActionPerformed(InputConstants.Cancel, HandleCancelPerformed);
            _playerInputManager.UnregisterActionPerformed(InputConstants.Detail, HandleDetailPerformed);

            // 取消监听选择事件
            GameApplication.Instance?.EventCenter.RemoveEventListener<CharacterObject>(
                GameEvents.FinishTargetSelection, OnFinishTargetSelection);

            // 取消监听相机场景变化
            _cameraModel.GetScene().RemoveObserver(OnCameraSceneChanged);

            UGUIUtil.DeselectIfSelectedInTargetChildren(_eventSystem, transform);
        }

        private GameObject SelectViewHolder()
        {
            return _rvBattleCommandList.RecyclerQuery.GetVisibleViewHolders()
                .OrderBy(viewHolder => viewHolder.Position)
                .FirstOrDefault()?.gameObject;
        }

        private void HandleNavigationMove(RecyclerViewHolder viewHolder, MoveDirection moveDirection)
        {
            if (!Focus || InTargetSelection)
            {
                return;
            }

            switch (moveDirection)
            {
                case MoveDirection.Up:
                    switch (viewHolder)
                    {
                        case GameBattleCommandListGroupViewHolder groupViewHolder:
                        {
                            if (_battleCommandModel.SelectPreviousGroup(viewHolder.Position, out var index,
                                    out var data))
                            {
                                adapter.RefreshItem(index, data);
                                _rvBattleCommandList.FocusItem(index, true);
                            }
                        }
                            break;
                        case GameBattleCommandListItemViewHolder itemViewHolder:
                        {
                            if (_battleCommandModel.SelectPreviousItem(viewHolder.Position, out var index,
                                    out var data))
                            {
                                adapter.RefreshItem(index, data);
                                _rvBattleCommandList.FocusItem(index, true);
                            }
                        }
                            break;
                    }

                    break;
                case MoveDirection.Down:
                    switch (viewHolder)
                    {
                        case GameBattleCommandListGroupViewHolder groupViewHolder:
                        {
                            if (_battleCommandModel.SelectNextGroup(viewHolder.Position, out var index,
                                    out var data))
                            {
                                adapter.RefreshItem(index, data);
                                _rvBattleCommandList.FocusItem(index, true);
                            }
                        }
                            break;
                        case GameBattleCommandListItemViewHolder itemViewHolder:
                        {
                            if (_battleCommandModel.SelectNextItem(viewHolder.Position, out var index,
                                    out var data))
                            {
                                adapter.RefreshItem(index, data);
                                _rvBattleCommandList.FocusItem(index, true);
                            }
                        }
                            break;
                    }

                    break;
            }
        }

        private void HandleBattleCommandClicked(object data)
        {
            if (!Focus || InTargetSelection)
            {
                return;
            }

            switch (data)
            {
                case GameBattleCommandGroupUIData groupData:
                {
                    if (groupData.Enable)
                    {
                        _battleCommandModel.GoToItemPage(groupData);
                        _eventSystem.SetSelectedGameObject(null);
                    }
                    else
                    {
                        Toast.Instance.Show(groupData.Skills.Count == 0 ? "该指令组暂无技能" : "角色无法使用该指令组");
                    }
                }
                    break;
                case GameBattleCommandItemUIData itemData:
                {
                    // 先判断是否满足技能预条件
                    if (Player.SkillAbility.MatchSkillPreconditions(itemData.Skill.id, itemData.SkillGroup,
                            out var failureReason))
                    {
                        // 如果能够释放技能，后续判断是否进入选择目标流程，无需选择就直接释放
                        if (itemData.NeedTarget)
                        {
                            // 获取技能可选目标列表
                            var selectableTargets = new List<CharacterObject>();
                            if ((itemData.TargetGroup & SkillTargetGroup.Self) != 0)
                            {
                                selectableTargets.Add(Player);
                            }
                            else if ((itemData.TargetGroup & SkillTargetGroup.Ally) != 0)
                            {
                                selectableTargets.AddRange(Player.BattleAbility.BattleAllies);
                            }
                            else if ((itemData.TargetGroup & SkillTargetGroup.Enemy) != 0)
                            {
                                selectableTargets.AddRange(Player.BattleAbility.BattleEnemies);
                            }

                            // 如果没有可选目标对象，就直接提示玩家
                            if (selectableTargets.Count == 0)
                            {
                                Toast.Instance.Show("没有可选对象", 2f);
                            }
                            else
                            {
                                // 如果仅存在玩家自身，就直接跳过选择目标流程，否则进入选择目标流程
                                if (selectableTargets.Count == 1 && selectableTargets[0] == Player)
                                {
                                    TryReleaseSkillWithTarget(itemData.Skill, Player);
                                }
                                else
                                {
                                    GameApplication.Instance.EventCenter.TriggerEvent(
                                        GameEvents.StartTargetSelection,
                                        selectableTargets.ToArray()
                                    );
                                }
                            }
                        }
                        else
                        {
                            TryReleaseSkillWithTarget(itemData.Skill, null);
                        }
                    }
                    else
                    {
                        // 提示玩家预条件失败原因
                        Toast.Instance.Show(failureReason.Message);
                    }
                }
                    break;
            }
        }

        private void HandlePageDataChanged(GameBattleCommandPageData pageData)
        {
            switch (pageData)
            {
                case GameBattleCommandCollapsePageData collapsePageData:
                {
                    _collapseLayout.gameObject.SetActive(true);
                    _expandLayout.gameObject.SetActive(false);
                    UGUIUtil.DeselectIfSelectedInTargetChildren(_eventSystem, transform);
                    // 收缩战斗指令时同步状态
                    _gameUIModel.BattleCommandExpanding.SetValue(false);
                }
                    break;
                case GameBattleCommandExpandPageData expandPageData:
                {
                    _collapseLayout.gameObject.SetActive(false);
                    _expandLayout.gameObject.SetActive(true);
                    _textBattleCommandTitle.text = expandPageData.Title;
                    // 展开战斗指令时同步状态
                    _gameUIModel.BattleCommandExpanding.SetValue(true);
                    if (Focus)
                    {
                        _eventSystem.SetSelectedGameObject(null);
                    }

                    switch (expandPageData)
                    {
                        case GameBattleCommandExpandGroupPageData expandGroupPageData:
                        {
                            _textDetailSwitch.gameObject.SetActive(false);
                            adapter.SetData(expandGroupPageData.Groups);
                        }
                            break;
                        case GameBattleCommandExpandItemPageData expandItemPageData:
                        {
                            _textDetailSwitch.gameObject.SetActive(true);
                            adapter.SetData(expandItemPageData.Items);
                        }
                            break;
                    }
                }
                    break;
                case GameBattleCommandHiddenPageData hiddenPageData:
                {
                    _collapseLayout.gameObject.SetActive(false);
                    _expandLayout.gameObject.SetActive(false);
                    UGUIUtil.DeselectIfSelectedInTargetChildren(_eventSystem, transform);
                }
                    break;
            }
        }

        private void HandleCommandPerformed(InputAction.CallbackContext obj)
        {
            if (!Focus || _battleCommandModel.GetPageData().Value is not GameBattleCommandCollapsePageData)
            {
                return;
            }

            _battleCommandModel.SwitchBetweenCollapseAndExpandPage();
        }

        private void HandleCancelPerformed(InputAction.CallbackContext callbackContext)
        {
            if (!Focus || _battleCommandModel.GetPageData().Value is not GameBattleCommandExpandPageData ||
                InTargetSelection)
            {
                return;
            }

            switch (_battleCommandModel.GetPageData().Value)
            {
                case GameBattleCommandExpandGroupPageData expandGroupPageData:
                {
                    _battleCommandModel.SwitchBetweenCollapseAndExpandPage();
                }
                    break;
                case GameBattleCommandExpandItemPageData expandItemPageData:
                {
                    _battleCommandModel.BackToGroupPage();
                }
                    break;
            }
        }

        private void HandleDetailPerformed(InputAction.CallbackContext callbackContext)
        {
            if (!Focus || _battleCommandModel.GetPageData().Value is not GameBattleCommandExpandItemPageData)
            {
                return;
            }

            _showDetail = !_showDetail;
        }

        private void OnFinishTargetSelection(CharacterObject target)
        {
            var itemViewHolder =
                _eventSystem.currentSelectedGameObject?.GetComponent<GameBattleCommandListItemViewHolder>();
            if (!itemViewHolder || itemViewHolder.Position == RecyclerView.NoPosition)
            {
                return;
            }

            TryReleaseSkillWithTarget(itemViewHolder.Data.Skill, target);
        }

        private void OnCameraSceneChanged(CameraSceneData cameraSceneData)
        {
            // 延迟一帧设置是否处于选择场景，避免退出选择时处理取消事件
            StartCoroutine(cameraSceneData.Scene == CameraScene.Selection ? SetSelection(true) : SetSelection(false));
            return;

            IEnumerator SetSelection(bool inSelection)
            {
                yield return 0;
                InTargetSelection = inSelection;
            }
        }

        private void TryReleaseSkillWithTarget(Skill.Runtime.Skill skill, CharacterObject target)
        {
            // 判断目标是否符合技能目标条件
            if (!Player.SkillAbility.MatchSkillTarget(skill.id, skill.group, target, out var targetFailureReason))
            {
                Toast.Instance.Show(targetFailureReason);
                return;
            }

            // 判断是否满足技能后条件
            if (Player.SkillAbility.MatchSkillPostconditions(skill.id, skill.group, target,
                    out var postconditionFailureReason))
            {
                if (!Player.SkillAbility.ReleaseSkill(skill.id, skill.group, target, out var releaseInfo))
                {
                    Toast.Instance.Show("技能释放失败");
                }
            }
            else
            {
                // 提示玩家预条件失败原因
                Toast.Instance.Show(postconditionFailureReason.Message);
            }
        }

        private void UpdateDetailPopup()
        {
            if (!_showDetail || _battleCommandModel.GetPageData().Value is not GameBattleCommandExpandItemPageData)
            {
                detailPopup.Hide();
                return;
            }

            var itemViewHolder =
                _eventSystem.currentSelectedGameObject?.GetComponent<GameBattleCommandListItemViewHolder>();
            if (!itemViewHolder || itemViewHolder.Position == RecyclerView.NoPosition || _rvBattleCommandList.Scrolling)
            {
                detailPopup.Hide();
                return;
            }

            // 展示物品信息
            detailPopup.Show(itemViewHolder.RectTransform, itemViewHolder.Data);
        }
    }
}