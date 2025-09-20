using System;
using Quest.Runtime;

namespace Quest.Data
{
    [Serializable]
    public class QuestGoalInfo
    {
        public string id; // 任务目标id
        [NonSerialized] public QuestGoalStart Start; // 任务目标开始函数委托
        [NonSerialized] public QuestGoalUpdate Update; // 任务目标更新函数委托
        [NonSerialized] public QuestGoalComplete Complete; // 任务目标完成函数委托
        [NonSerialized] public QuestGoalInterrupt Interrupt; // 任务目标中断函数委托
    }

    public delegate void QuestGoalStart(QuestGoal goal);

    public delegate void QuestGoalUpdate(QuestGoal goal, float deltaTime);

    public delegate void QuestGoalComplete(QuestGoal goal);

    public delegate void QuestGoalInterrupt(QuestGoal goal);
}