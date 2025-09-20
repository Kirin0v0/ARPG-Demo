using System;
using Quest.Data;

namespace Quest.Runtime
{
    [Serializable]
    public class QuestGoal
    {
        public QuestGoalInfo info; // 任务目标配置信息
        public string description; // 任务目标描述
        public bool triggered; // 任务目标是否触发
        public bool completed; // 任务目标是否完成
        public string state; // 任务目标状态，本质上是序列化后的数据字符串，用于数据持久化
    }
}