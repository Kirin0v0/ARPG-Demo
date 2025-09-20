using Character;
using NodeCanvas.Framework;
using ParadoxNotion.Design;
using Quest;
using UnityEngine;

namespace Dialogue.Task
{
    [Category("Quest")]
    public class DialogueReceiveQuest : ActionTask
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
        
        protected override string info => "Receive quest " + id;

        protected override void OnExecute()
        {
            if (!QuestManager)
            {
                EndAction();
                return;
            }
            
            QuestManager.ReceiveQuest(id.value);
            EndAction();
        }
    }
}