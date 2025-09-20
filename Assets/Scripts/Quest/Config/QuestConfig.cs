using System;
using System.Collections.Generic;
using System.Linq;
using Quest.Config.Goal;
using Quest.Config.Requirement;
using Quest.Config.Reward;
using Quest.Config.Step;
using Quest.Data;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Quest.Config
{
    public class QuestConfig : SerializedScriptableObject
    {
        [Title("任务通用配置"), ReadOnly] public string id = ""; // 任务id，这个由编辑器来生成
        public string title = ""; // 任务标题
        [TextArea] public string description = ""; // 任务描述
        [TextArea] public string awaitSubmitDescription = ""; // 任务等待提交的描述
        [TextArea] public string completedDescription = ""; // 任务完成后的描述

        [Title("任务需求列表"), TypeFilter("GetRequirementsFilteredTypeList")]
        public List<BaseQuestRequirement> requirements = new();

        [Title("任务步骤列表"), TypeFilter("GetStepsFilteredTypeList")]
        public List<BaseQuestStep> steps = new();

        [Title("任务奖励列表"), TypeFilter("GetRewardsFilteredTypeList")]
        public List<BaseQuestReward> rewards = new();

        public QuestInfo ToQuestInfo()
        {
            return new QuestInfo
            {
                id = id,
                title = title,
                description = description,
                awaitSubmitDescription = awaitSubmitDescription,
                completedDescription = completedDescription,
                requirements = requirements.Select(ToQuestRequirementInfo).ToArray(),
                steps = steps.Select(ToQuestStepInfo).ToArray(),
                rewards = rewards.Select(ToQuestRewardInfo).ToArray()
            };
        }

        private QuestRequirementInfo ToQuestRequirementInfo(BaseQuestRequirement requirement)
        {
            return new QuestRequirementInfo
            {
                id = requirement.id,
                Update = requirement.Update
            };
        }

        private QuestStepInfo ToQuestStepInfo(BaseQuestStep step)
        {
            return new QuestStepInfo
            {
                id = step.id,
                description = step.description,
                goalRelation = step switch
                {
                    QuestParallelAndStep => QuestStepGoalRelation.ParallelAnd,
                    QuestParallelOrStep => QuestStepGoalRelation.ParallelOr,
                    _ => QuestStepGoalRelation.Linear
                },
                goals = step.goals.Select(ToQuestGoalInfo).ToArray(),
                Start = step.Start,
                Update = step.Update,
                Complete = step.Complete,
                Interrupt = step.Interrupt,
            };
        }

        private QuestGoalInfo ToQuestGoalInfo(BaseQuestGoal goal)
        {
            return new QuestGoalInfo
            {
                id = goal.id,
                Start = goal.Start,
                Update = goal.Update,
                Complete = goal.Complete,
                Interrupt = goal.Interrupt,
            };
        }

        private QuestRewardInfo ToQuestRewardInfo(BaseQuestReward reward)
        {
            return new QuestRewardInfo
            {
                id = reward.id,
                GiveReward = reward.GiveQuestReward,
            };
        }

        private IEnumerable<Type> GetRequirementsFilteredTypeList()
        {
            var q = typeof(BaseQuestRequirement).Assembly.GetTypes()
                .Where(x => !x.IsAbstract)
                .Where(x => !x.IsGenericTypeDefinition)
                .Where(x => typeof(BaseQuestRequirement).IsAssignableFrom(x));
            return q;
        }

        private IEnumerable<Type> GetStepsFilteredTypeList()
        {
            var q = typeof(BaseQuestStep).Assembly.GetTypes()
                .Where(x => !x.IsAbstract)
                .Where(x => !x.IsGenericTypeDefinition)
                .Where(x => typeof(BaseQuestStep).IsAssignableFrom(x));
            return q;
        }

        private IEnumerable<Type> GetRewardsFilteredTypeList()
        {
            var q = typeof(BaseQuestReward).Assembly.GetTypes()
                .Where(x => !x.IsAbstract)
                .Where(x => !x.IsGenericTypeDefinition)
                .Where(x => typeof(BaseQuestReward).IsAssignableFrom(x));
            return q;
        }
    }
}