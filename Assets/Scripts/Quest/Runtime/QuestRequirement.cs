using System;
using Quest.Data;

namespace Quest.Runtime
{
    [Serializable]
    public class QuestRequirement
    {
        public QuestRequirementInfo info; // 任务需求配置信息
        public string description; //  任务需求描述
        public bool meet; // 任务需求是否满足
    }
}