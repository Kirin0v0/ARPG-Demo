using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Common;
using Features.Game.Data;
using Framework.Common.Debug;
using Framework.Common.UI.Panel;
using Framework.Common.UI.RecyclerView;
using Framework.Common.UI.RecyclerView.LayoutManager;
using Framework.Common.Util;
using Inputs;
using Quest;
using Quest.Config.Step;
using Quest.Data;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using UnityEngine.UI;
using VContainer;
using PlayerInputManager = Inputs.PlayerInputManager;

namespace Features.Game.UI.Quest
{
    public class GameQuestPanel : BaseUGUIPanel
    {
        [Inject] private PlayerInputManager _playerInputManager;
        [Inject] private InputInfoManager _inputInfoManager;
        [Inject] private QuestManager _questManager;
        [Inject] private GameUIModel _gameUIModel;
        [Inject] private EventSystem _eventSystem;

        #region 头部栏

        private Button _btnBack;

        #endregion

        #region 标签栏

        private Image _imgTabAllNormal;
        private Image _imgTabAllSelected;
        private Image _imgTabInProgressNormal;
        private Image _imgTabInProgressSelected;
        private Image _imgTabCompletedNormal;
        private Image _imgTabCompletedSelected;

        #endregion

        #region 内容布局

        private TextMeshProUGUI _textEmpty;

        private RectTransform _questListLayout;
        private RecyclerView _rvQuestList;
        [Title("任务列表配置")] [SerializeField] private RecyclerViewSelectable questListSelectable;
        [SerializeField] private RecyclerViewLayoutManager questListLayoutManager;
        [SerializeField] private GameQuestListAdapter questListAdapter;

        private RectTransform _questDetailLayout;
        private RecyclerView _rvQuestDetail;
        [Title("任务详情配置")] [SerializeField] private RecyclerViewLayoutManager questDetailLayoutManager;
        [SerializeField] private GameQuestDetailAdapter questDetailAdapter;

        [FormerlySerializedAs("scrollRatio")] [SerializeField]
        private float questDetailScrollRatio = 0.1f;

        #endregion

        private TextMeshProUGUI _textSubmit;

        private GameQuestModel _questModel;

        protected override void OnInit()
        {
            _btnBack = GetWidget<Button>("BtnBack");

            _imgTabAllNormal = GetWidget<Image>("ImgTabAllNormal");
            _imgTabAllSelected = GetWidget<Image>("ImgTabAllSelected");
            _imgTabInProgressNormal = GetWidget<Image>("ImgTabInProgressNormal");
            _imgTabInProgressSelected = GetWidget<Image>("ImgTabInProgressSelected");
            _imgTabCompletedNormal = GetWidget<Image>("ImgTabCompletedNormal");
            _imgTabCompletedSelected = GetWidget<Image>("ImgTabCompletedSelected");

            _questListLayout = GetWidget<RectTransform>("QuestListLayout");
            _questDetailLayout = GetWidget<RectTransform>("QuestDetailLayout");
            _textEmpty = GetWidget<TextMeshProUGUI>("TextEmpty");
            _textSubmit = GetWidget<TextMeshProUGUI>("TextSubmit");

            _rvQuestList = GetWidget<RecyclerView>("RvQuestList");
            _rvQuestList.Init();
            _rvQuestList.LayoutManager = questListLayoutManager;
            questListAdapter.Init(HandleNavigationSelected, HandleNavigationDeselected, HandleNavigationMove);
            _rvQuestList.Adapter = questListAdapter;

            _rvQuestDetail = GetWidget<RecyclerView>("RvQuestDetail");
            _rvQuestDetail.Init();
            _rvQuestDetail.LayoutManager = questDetailLayoutManager;
            _rvQuestDetail.Adapter = questDetailAdapter;
        }

        protected override void OnShow(object payload)
        {
            _questModel = new GameQuestModel(_questManager);

            // 监听返回按钮点击事件
            _btnBack.onClick.AddListener(HandleButtonBackClicked);

            // 监听玩家输入
            _playerInputManager.RegisterActionPerformed(InputConstants.Previous, HandlePreviousPerformed);
            _playerInputManager.RegisterActionPerformed(InputConstants.Next, HandleNextPerformed);
            _playerInputManager.RegisterActionPerformed(InputConstants.Scroll, HandleScrollPerformed);

            // 监听页面数据变化
            _questModel.GetTab().ObserveForever(HandleTabTypeChanged);
            _questModel.GetQuestList().ObserveForever(HandleTabQuestListChanged);
            _questModel.GetSelectedQuest().ObserveForever(HandleSelectedQuestChanged);

            // 由于HorizontalGroup的问题在第一帧列表的宽高会重置为(0,0)，因此等待内部计算宽高结束后再展示列表
            StartCoroutine(LateShowDefaultUI());

            _rvQuestList.ScrollToPosition(0);
            _rvQuestDetail.ScrollToPosition(0);
            if (Focus)
            {
                _eventSystem.SetSelectedGameObject(null);
            }
        }

        protected override void OnShowingUpdate(bool focus)
        {
            // 如果未选中页面元素则在导航时设置默认选中
            if (focus && !_eventSystem.currentSelectedGameObject &&
                _playerInputManager.WasPerformedThisFrame(InputConstants.Navigate))
            {
                _eventSystem.SetSelectedGameObject(_btnBack.gameObject);
            }

            // 按下取消键则选中返回按钮
            if (focus && _playerInputManager.WasPerformedThisFrame(InputConstants.Cancel))
            {
                _eventSystem.SetSelectedGameObject(_btnBack.gameObject);
            }

            // 如果选中返回按钮则展示提示，否则就隐藏提示
            _textSubmit.gameObject.SetActive(_eventSystem.currentSelectedGameObject == _btnBack.gameObject);
        }

        protected override void OnHide()
        {
            // 取消监听返回按钮点击事件
            _btnBack.onClick.RemoveListener(HandleButtonBackClicked);

            // 取消监听玩家输入
            _playerInputManager.UnregisterActionPerformed(InputConstants.Previous, HandlePreviousPerformed);
            _playerInputManager.UnregisterActionPerformed(InputConstants.Next, HandleNextPerformed);
            _playerInputManager.UnregisterActionPerformed(InputConstants.Scroll, HandleScrollPerformed);

            // 取消监听页面数据变化
            _questModel.GetTab().RemoveObserver(HandleTabTypeChanged);
            _questModel.GetQuestList().RemoveObserver(HandleTabQuestListChanged);
            _questModel.GetSelectedQuest().RemoveObserver(HandleSelectedQuestChanged);
            _questModel = null;

            UGUIUtil.DeselectIfSelectedInTargetChildren(_eventSystem, transform);
        }

        private IEnumerator LateShowDefaultUI()
        {
            yield return new WaitForNextFrameUnit();
            _questModel.SwitchTab(GameQuestTab.All);
        }

        private void HandleNavigationSelected(RecyclerViewHolder viewHolder)
        {
            if (viewHolder is not GameQuestItemViewHolder questItemViewHolder || questItemViewHolder.Data == null)
            {
                return;
            }

            _questModel.SelectQuest(questItemViewHolder.Position, questItemViewHolder.Data);
        }

        private void HandleNavigationDeselected(RecyclerViewHolder viewHolder)
        {
            if (viewHolder is not GameQuestItemViewHolder questItemViewHolder || questItemViewHolder.Data == null)
            {
                return;
            }

            if (_questModel.GetSelectedQuest().Value.data == questItemViewHolder.Data)
            {
                _questModel.SelectQuest(-1, default);
            }
        }

        private void HandleNavigationMove(RecyclerViewHolder viewHolder, MoveDirection moveDirection)
        {
            switch (moveDirection)
            {
                case MoveDirection.Up:
                {
                    if (_questModel.SelectPreviousItem(viewHolder.Position, out var index, out var data))
                    {
                        questListAdapter.RefreshItem(index, data);
                        _rvQuestList.FocusItem(index, true);
                    }
                    else
                    {
                        _eventSystem.SetSelectedGameObject(questListSelectable.navigation.selectOnUp
                            ?.gameObject);
                    }
                }
                    break;
                case MoveDirection.Down:
                {
                    if (_questModel.SelectNextItem(viewHolder.Position, out var index, out var data))
                    {
                        questListAdapter.RefreshItem(index, data);
                        _rvQuestList.FocusItem(index, true);
                    }
                }
                    break;
            }
        }

        private void HandleButtonBackClicked()
        {
            _gameUIModel.QuestUI.SetValue(_gameUIModel.QuestUI.Value.Close());
        }

        private void HandlePreviousPerformed(InputAction.CallbackContext obj)
        {
            if (!Focus)
            {
                return;
            }

            _questModel.SwitchToPreviousTab();
        }

        private void HandleNextPerformed(InputAction.CallbackContext obj)
        {
            if (!Focus)
            {
                return;
            }

            _questModel.SwitchToNextTab();
        }

        private void HandleScrollPerformed(InputAction.CallbackContext obj)
        {
            if (!Focus)
            {
                return;
            }

            _rvQuestDetail.ScrollDelta(-obj.ReadValue<float>());
        }

        private void HandleTabTypeChanged(GameQuestTab tab)
        {
            _imgTabAllSelected.gameObject.SetActive(false);
            _imgTabInProgressSelected.gameObject.SetActive(false);
            _imgTabCompletedSelected.gameObject.SetActive(false);
            _imgTabAllNormal.gameObject.SetActive(true);
            _imgTabInProgressNormal.gameObject.SetActive(true);
            _imgTabCompletedNormal.gameObject.SetActive(true);

            switch (tab)
            {
                case GameQuestTab.All:
                    _imgTabAllNormal.gameObject.SetActive(false);
                    _imgTabAllSelected.gameObject.SetActive(true);
                    break;
                case GameQuestTab.InProgress:
                    _imgTabInProgressNormal.gameObject.SetActive(false);
                    _imgTabInProgressSelected.gameObject.SetActive(true);
                    break;
                case GameQuestTab.Completed:
                    _imgTabCompletedNormal.gameObject.SetActive(false);
                    _imgTabCompletedSelected.gameObject.SetActive(true);
                    break;
            }
        }

        private void HandleTabQuestListChanged(List<GameQuestItemUIData> questList)
        {
            SetListUIVisibility(questList.Count != 0);
            questListAdapter.SetData(questList);
        }

        private void HandleSelectedQuestChanged((int position, GameQuestItemUIData data) pair)
        {
            if (pair.position < 0)
            {
                questDetailAdapter.SetData(new List<object>());
                return;
            }

            // 左侧列表聚焦子项
            _rvQuestList.FocusItem(pair.position, true);
            // 处理数据并设置到右侧详细列表中
            questDetailAdapter.SetData(ProcessDetailDataList());

            return;

            List<object> ProcessDetailDataList()
            {
                var quest = pair.data.Quest;
                var detailData = new List<object>
                {
                    new GameQuestDetailHeaderUIData
                    {
                        Title = quest.info.title,
                        Description = quest.info.description,
                        Requirements = quest.requirements.Select(requirement => requirement.description).ToList(),
                    }
                };
                for (var i = 0; i <= quest.StepIndex && i < quest.steps.Length; i++)
                {
                    var step = quest.steps[i];
                    detailData.Add(new GameQuestDetailStepUIData
                    {
                        Index = i,
                        ShowIndex = quest.steps.Length > 1,
                        Description = step.info.description,
                        Completed = step.completed,
                        Relation = step.info.goalRelation
                    });
                    var toShowGoalEndIndex = step.goals.Length - 1;
                    if (step.info.goalRelation == QuestStepGoalRelation.Linear)
                    {
                        for (var j = 0; j < step.goals.Length; j++)
                        {
                            var goal = step.goals[j];
                            if (!goal.completed)
                            {
                                toShowGoalEndIndex = j;
                                break;
                            }
                        }
                    }

                    for (var j = 0; j <= toShowGoalEndIndex && j < step.goals.Length; j++)
                    {
                        var goal = step.goals[j];
                        detailData.Add(new GameQuestDetailGoalUIData
                        {
                            Index = j,
                            ShowIndex = step.goals.Length > 1,
                            Description = goal.description,
                            Completed = goal.completed,
                            Relation = step.info.goalRelation
                        });
                    }
                }

                if (quest.StepIndex >= quest.steps.Length)
                {
                    detailData.Add(new GameQuestDetailFooterUIData
                    {
                        Description = quest.state.IsQuestCompleted()
                            ? quest.info.completedDescription
                            : quest.info.awaitSubmitDescription
                    });
                }

                return detailData;
            }
        }

        private void SetListUIVisibility(bool visible)
        {
            if (!visible)
            {
                _questListLayout.gameObject.SetActive(false);
                _questDetailLayout.gameObject.SetActive(false);
                _textEmpty.gameObject.SetActive(true);
            }
            else
            {
                _questListLayout.gameObject.SetActive(true);
                _questDetailLayout.gameObject.SetActive(true);
                _textEmpty.gameObject.SetActive(false);
            }
        }
    }
}