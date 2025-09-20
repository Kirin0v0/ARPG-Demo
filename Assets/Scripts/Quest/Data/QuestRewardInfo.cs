using System;
using Character;

namespace Quest.Data
{
    [Serializable]
    public class QuestRewardInfo
    {
        public string id; // 任务奖励id
        [NonSerialized] public GiveQuestReward GiveReward; // 任务奖励给予函数委托
    }

    public delegate void GiveQuestReward();
}