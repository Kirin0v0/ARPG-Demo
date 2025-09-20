using System;
using Quest.Data;

namespace Quest.Runtime
{
    [Serializable]
    public class QuestReward
    {
        public QuestRewardInfo info; // 任务奖励配置信息
        public bool given; // 任务奖励是否给予
    }
}