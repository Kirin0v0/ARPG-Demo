using System;
using Quest.Runtime;

namespace Quest.Data
{
    [Serializable]
    public class QuestRequirementInfo
    {
        public string id; // 任务需求id
        [NonSerialized] public QuestRequirementUpdate Update; // 任务需求更新函数委托
    }

    public delegate void QuestRequirementUpdate(QuestRequirement requirement, float deltaTime);
}