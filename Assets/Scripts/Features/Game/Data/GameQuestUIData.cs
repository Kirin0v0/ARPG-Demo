using System.Collections.Generic;
using Quest.Config.Step;

namespace Features.Game.Data
{
    public class GameQuestItemUIData
    {
        public Quest.Runtime.Quest Quest;
        public bool Selected;
    }
    
    public class GameQuestDetailHeaderUIData
    {
        public string Title;
        public string Description;
        public List<string> Requirements;
    }

    public class GameQuestDetailStepUIData
    {
        public int Index; // 从0开始
        public bool ShowIndex;
        public string Description;
        public bool Completed;
        public QuestStepGoalRelation Relation;
    }

    public class GameQuestDetailGoalUIData
    {
        public int Index; // 从0开始
        public bool ShowIndex;
        public string Description;
        public bool Completed;
        public QuestStepGoalRelation Relation;
    }

    public class GameQuestDetailFooterUIData
    {
        public string Description;
    }
}