using System;
using System.Collections.Generic;
using System.Linq;
using Features.Game.Data;
using Framework.Core.LiveData;
using Quest;
using Quest.Data;

namespace Features.Game.UI.Quest
{
    public enum GameQuestTab
    {
        All,
        InProgress,
        Completed,
    }

    public class GameQuestModel
    {
        private readonly QuestManager _questManager;

        private readonly MutableLiveData<GameQuestTab> _tabLiveData = new();
        public LiveData<GameQuestTab> GetTab() => _tabLiveData;

        private readonly MutableLiveData<List<GameQuestItemUIData>> _quests = new();
        public LiveData<List<GameQuestItemUIData>> GetQuestList() => _quests;

        private readonly MutableLiveData<(int position, GameQuestItemUIData data)> _selectedQuest = new();
        public LiveData<(int position, GameQuestItemUIData data)> GetSelectedQuest() => _selectedQuest;

        public GameQuestModel(QuestManager questManager)
        {
            _questManager = questManager;
        }

        public void SwitchTab(GameQuestTab tab)
        {
            // 设置Tab类型
            _tabLiveData.SetValue(tab);
            // 过滤任务列表
            FilterQuestList();
        }

        public void SwitchToPreviousTab()
        {
            var previousValue = (int)_tabLiveData.Value - 1;
            if (previousValue < 0)
            {
                previousValue = Enum.GetValues(typeof(GameQuestTab)).Length - 1; // 回到最后一个枚举值
            }

            SwitchTab((GameQuestTab)previousValue);
        }

        public void SwitchToNextTab()
        {
            var nextValue = (int)_tabLiveData.Value + 1;
            if (nextValue >= Enum.GetValues(typeof(GameQuestTab)).Length)
            {
                nextValue = 0; // 回到第一个枚举值
            }

            SwitchTab((GameQuestTab)nextValue);
        }

        public void SelectQuest(int position, GameQuestItemUIData data)
        {
            _selectedQuest.SetValue((position, data));
        }

        public bool SelectPreviousItem(int position, out int index, out GameQuestItemUIData data)
        {
            index = -1;
            data = null;
            if (position < 0 || position >= _quests.Value.Count || position - 1 < 0)
            {
                return false;
            }

            index = position - 1;
            data = _quests.Value[index];
            data.Selected = true;
            return true;
        }

        public bool SelectNextItem(int position, out int index, out GameQuestItemUIData data)
        {
            index = -1;
            data = null;
            if (position < 0 || position >= _quests.Value.Count || position + 1 >= _quests.Value.Count)
            {
                return false;
            }

            index = position + 1;
            data = _quests.Value[index];
            data.Selected = true;
            return true;
        }

        private void FilterQuestList()
        {
            switch (_tabLiveData.Value)
            {
                case GameQuestTab.All:
                {
                    var quests = _questManager.PlayerQuests
                        .Select((quest, index) => new GameQuestItemUIData
                        {
                            Quest = quest,
                        }).ToList();
                    _selectedQuest.SetValue((-1, default));
                    _quests.SetValue(quests);
                }
                    break;
                case GameQuestTab.InProgress:
                {
                    var quests = _questManager.PlayerQuests
                        .Where(quest => quest.state.IsQuestInProgress())
                        .Select((quest, index) => new GameQuestItemUIData
                        {
                            Quest = quest,
                        }).ToList();
                    _selectedQuest.SetValue((-1, default));
                    _quests.SetValue(quests);
                }
                    break;
                case GameQuestTab.Completed:
                {
                    var quests = _questManager.PlayerQuests
                        .Where(quest => quest.state.IsQuestCompleted())
                        .Select((quest, index) => new GameQuestItemUIData
                        {
                            Quest = quest,
                        }).ToList();
                    _selectedQuest.SetValue((-1, default));
                    _quests.SetValue(quests);
                }
                    break;
            }
        }
    }
}