using System;
using System.Collections.Generic;
using System.Linq;
using Quest.Runtime;
using Sirenix.Utilities;

namespace Quest.Config.Step
{
    /// <summary>
    /// 并行+与的任务步骤
    /// 当且仅当全部目标都完成时才会执行目标完成函数，并设置为步骤完成，除了以上情况外会一直更新目标完成状态
    /// </summary>
    [Serializable]
    public class QuestParallelAndStep : BaseQuestStep
    {
        private List<QuestGoal> _goals = new();
        
        public override void Start(QuestStep step)
        {
            // 在开始时遍历所有目标，由于存在部分目标反序列化已完成，这里仅执行未完成目标的生命周期函数
            _goals ??= new();
            _goals.Clear();
            step.goals.ForEach(goal =>
            {
                if (goal.completed)
                {
                    goal.triggered = true;
                }
                else
                {
                    _goals.Add(goal);
                }
            });
            
            // 如果任务目标数量为0，则任务步骤已经完成
            if (_goals.Count == 0)
            {
                step.completed = true;
                return;
            }
            
            // 遍历所有目标，如果目标已完成则跳过，如果未完成则执行完整生命周期函数
            _goals.ForEach(goal =>
            {
                goal.triggered = true;
                goal.info.Start?.Invoke(goal);
            });
        }

        public override void Update(QuestStep step, float deltaTime)
        {
            // 更新所有未完成的目标
            _goals.ForEach(goal =>
            {
                if (!goal.completed)
                {
                    goal.info.Update?.Invoke(goal, deltaTime);
                }
            });
            // 如果全部目标都完成，就认为步骤完成，否则认为步骤未完成
            step.completed = _goals.All(goal => goal.completed);
        }

        public override void Complete(QuestStep step)
        {
            // 完成所有目标
            _goals.ForEach(goal => goal.info.Complete?.Invoke(goal));
        }

        public override void Interrupt(QuestStep step)
        {
            // 遍历所有目标，如果目标已触发但未完成，则执行中断生命周期函数
            _goals.ForEach(goal =>
            {
                if (goal.triggered && !goal.completed)
                {
                    goal.info.Interrupt?.Invoke(goal);
                }
            });
        }
    }
}