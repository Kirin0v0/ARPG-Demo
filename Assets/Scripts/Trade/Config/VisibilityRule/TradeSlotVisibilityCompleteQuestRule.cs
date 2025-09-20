using System;
using Character;
using Quest;
using Quest.Data;
using UnityEngine;
using VContainer;

namespace Trade.Config.VisibilityRule
{
    [System.Serializable]
    public class TradeSlotVisibilityCompleteQuestRule : BaseTradeSlotVisibilityRule
    {
        private QuestManager _questManager;

        private QuestManager QuestManager
        {
            get
            {
                if (!_questManager)
                {
                    _questManager = GameEnvironment.FindEnvironmentComponent<QuestManager>();
                }

                return _questManager;
            }
        }

        public string questId;

        public override bool SetSlotVisibility(CharacterObject self, CharacterObject target)
        {
            if (!QuestManager)
            {
                return false;
            }

            return QuestManager.TryGetQuest(questId, out var quest) && quest.state.IsQuestCompleted();
        }
    }
}