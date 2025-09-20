namespace Quest.Data
{
    public enum QuestState
    {
        RequirementNotMeet = 1 << 0,
        RequirementMeet = 1 << 1,
        InProgress = 1 << 2,
        AllStepsComplete = 1 << 3,
        Completed = 1 << 4,
        RewardGiven = 1 << 5,
    }

    public static class QuestStateExtension
    {
        public static bool IsQuestNotStart(this QuestState questState)
        {
            return questState < QuestState.InProgress;
        }
        
        public static bool IsQuestInProgress(this QuestState questState)
        {
            return questState is >= QuestState.InProgress and < QuestState.Completed;
        }
        
        public static bool IsQuestCompleted(this QuestState questState)
        {
            return questState >= QuestState.Completed;
        }
    }
}