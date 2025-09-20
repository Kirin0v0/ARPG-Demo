using System;
using Quest.Data;

namespace Quest.Runtime
{
    [Serializable]
    public class QuestStep
    {
        public QuestStepInfo info; // 任务步骤配置信息
        public bool triggered; // 任务步骤是否触发
        public bool completed; // 任务步骤是否完成
        public QuestGoal[] goals; // 任务步骤目标运行数据数组
    }
}