using System;
using Quest.Data;

namespace Quest.Runtime
{
    [Serializable]
    public class Quest
    {
        public QuestInfo info; // 任务配置信息，这个数据是预设数据，不可改变
        public QuestState state; // 任务状态
        public QuestRequirement[] requirements; // 任务需求运行数据数组
        public QuestStep[] steps; // 任务步骤运行数据数组
        public QuestReward[] rewards; // 任务奖励运行数据数组

        /// <summary>
        /// 任务步骤索引，如果全部任务步骤都完成，则返回步骤数组长度，默认返回线性的未完成的步骤索引
        /// </summary>
        public int StepIndex
        {
            get
            {
                for (var i = 0; i < steps.Length; i++)
                {
                    var step = steps[i];
                    if (!step.completed)
                    {
                        return i;
                    }
                }

                return steps.Length;
            }
        }
    }
}