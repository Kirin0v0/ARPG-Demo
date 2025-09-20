using System;
using System.Collections.Generic;
using System.Linq;
using Quest.Config.Goal;
using Quest.Data;
using Quest.Runtime;
using Sirenix.OdinInspector;

namespace Quest.Config.Step
{
    public enum QuestStepGoalRelation
    {
        Linear,
        ParallelAnd,
        ParallelOr,
    }
    
    [Serializable]
    public abstract class BaseQuestStep
    {
        [InlineButton("ResetId")] public string id = Guid.NewGuid().ToString();
        
        [Title("任务步骤通用配置")] public string description;

        [Title("任务步骤目标列表"), TypeFilter("GetGoalFilteredTypeList")]
        public List<BaseQuestGoal> goals = new();
        
        public abstract void Start(QuestStep step); // 开始步骤函数节点

        public abstract void Update(QuestStep step, float deltaTime); // 更新步骤函数节点

        public abstract void Complete(QuestStep step); // 完成步骤函数节点

        public abstract void Interrupt(QuestStep step); // 中断步骤函数节点，用于清除正在阻塞的任务步骤

        private IEnumerable<Type> GetGoalFilteredTypeList()
        {
            var q = typeof(BaseQuestGoal).Assembly.GetTypes()
                .Where(x => !x.IsAbstract)
                .Where(x => !x.IsGenericTypeDefinition)
                .Where(x => typeof(BaseQuestGoal).IsAssignableFrom(x));
            return q;
        }

        private void ResetId()
        {
            id = Guid.NewGuid().ToString();
        }
    }
}