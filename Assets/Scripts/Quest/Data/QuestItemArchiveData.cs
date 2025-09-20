using System;
using System.Collections.Generic;

namespace Quest.Data
{
    [Serializable]
    public class QuestArchiveData
    {
        public List<QuestItemArchiveData> quests = new();
    }
    
    [Serializable]
    public class QuestItemArchiveData
    {
        public string id = "";
        public QuestState state = QuestState.RequirementNotMeet;
        public Dictionary<string, QuestRequirementArchiveData> requirements = new();
        public Dictionary<string, QuestStepArchiveData> steps = new();
        public Dictionary<string, QuestRewardArchiveData> rewards = new();
    }

    [Serializable]
    public class QuestRequirementArchiveData
    {
        public string id = "";
        public string description = "";
        public bool meet = false;
    }

    [Serializable]
    public class QuestStepArchiveData
    {
        public string id = "";
        public bool completed = false;
        public Dictionary<string, QuestGoalArchiveData> goals = new();
    }

    [Serializable]
    public class QuestGoalArchiveData
    {
        public string id = "";
        public string description = "";
        public bool completed = false;
        public string state = "";
    }

    [Serializable]
    public class QuestRewardArchiveData
    {
        public string id = "";
        public bool given = false;
    }
}