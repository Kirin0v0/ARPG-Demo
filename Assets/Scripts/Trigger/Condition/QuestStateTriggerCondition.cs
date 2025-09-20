using Character;
using Framework.Common.Trigger;
using Framework.Common.Trigger.Chain;
using Quest;
using Quest.Data;
using UnityEngine;
using VContainer;

namespace Trigger.Condition
{
    public class QuestStateTriggerCondition : BaseTriggerCondition<CharacterObject>
    {
        [SerializeField] private string questId;
        [SerializeField] private QuestState matchState = QuestState.Completed;

        [Inject] private QuestManager _questManager;

        protected override bool MatchCondition(CharacterObject character)
        {
            if (_questManager.TryGetQuest(questId, out var quest))
            {
                return quest.state >= matchState;
            }

            return false;
        }

        protected override BaseTriggerCondition<CharacterObject> OnClone(GameObject gameObject)
        {
            gameObject.name = "Quest State Trigger Condition";
            var triggerCondition = gameObject.AddComponent<QuestStateTriggerCondition>();
            triggerCondition.questId = questId;
            triggerCondition.matchState = matchState;
            triggerCondition._questManager = _questManager;
            return triggerCondition;
        }
    }
}