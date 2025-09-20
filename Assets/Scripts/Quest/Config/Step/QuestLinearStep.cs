using System;
using Quest.Runtime;
using Sirenix.Utilities;

namespace Quest.Config.Step
{
    /// <summary>
    /// 串行的任务步骤
    /// 当上一个目标完成后才会执行下一个目标的更新函数，直到最后一个目标完成后才会认为步骤完成，此外，在目标完成后不再执行其更新函数
    /// </summary>
    [Serializable]
    public class QuestLinearStep : BaseQuestStep
    {
        public override void Start(QuestStep step)
        {
        }

        public override void Update(QuestStep step, float deltaTime)
        {
            // 如果任务目标数量为0，则任务步骤已经完成
            if (step.goals.Length == 0)
            {
                step.completed = true;
                return;
            }

            // 依次遍历任务目标执行未触发的目标的对应节点函数
            for (var index = 0; index < step.goals.Length; index++)
            {
                var goal = step.goals[index];
                // 如果目标已经完成，跳过该任务目标
                var triggered = goal.triggered;
                if (goal.completed)
                {
                    // 设置为触发状态
                    if (!triggered)
                    {
                        goal.triggered = true;
                    }

                    // 目标完成就跳到下一个目标
                    continue;
                }

                // 目标未触发则需要执行目标的开始节点函数
                if (!triggered)
                {
                    goal.info.Start?.Invoke(goal);
                    goal.triggered = true;
                }

                // 执行目标的更新节点函数
                goal.info.Update?.Invoke(goal, triggered ? deltaTime : 0);

                // 在更新后判断目标是否完成，如果未完成则直接返回，即任务卡在这一目标了
                if (!goal.completed)
                {
                    return;
                }

                // 完成了就执行目标的完成节点函数，并跳到下一目标
                goal.info.Complete?.Invoke(goal);
            }

            // 到达这里说明全部任务目标已经完成，设置为完成步骤
            step.completed = true;
        }

        public override void Complete(QuestStep step)
        {
        }

        public override void Interrupt(QuestStep step)
        {
            // 依次遍历任务目标，遇到触发且未完成的目标则执行其中断节点函数，并直接返回
            for (var index = 0; index < step.goals.Length; index++)
            {
                var goal = step.goals[index];
                if (goal.triggered && !goal.completed)
                {
                    goal.info.Interrupt?.Invoke(goal);
                    return;
                }
            }
        }
    }
}