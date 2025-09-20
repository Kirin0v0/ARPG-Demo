using NodeCanvas.Framework;
using ParadoxNotion.Design;
using Quest;
using Quest.Data;
using UnityEngine;

namespace Dialogue.Task
{
    [Category("Quest")]
    public class DialogueCheckQuestAllowSubmit : ConditionTask
    {
        [RequiredField] public BBParameter<string> id;

        private QuestManager _questManager;

        private QuestManager QuestManager
        {
            get
            {
                if (_questManager)
                {
                    return _questManager;
                }

                _questManager = GameEnvironment.FindEnvironmentComponent<QuestManager>();
                return _questManager;
            }
        }

        protected override string info => $"Check quest(id={id}) whether allow submit";

        protected override bool OnCheck()
        {
            if (!QuestManager)
            {
                return false;
            }

            if (QuestManager.TryGetQuest(id.value, out var quest))
            {
                return quest.state == QuestState.AllStepsComplete;
            }

            return false;
        }
    }
}