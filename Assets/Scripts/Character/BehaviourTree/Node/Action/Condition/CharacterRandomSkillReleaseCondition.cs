using System;
using System.Collections.Generic;
using Framework.Common.BehaviourTree.Node;
using Framework.Common.BehaviourTree.Node.Action;
using Framework.Common.Debug;
using Sirenix.OdinInspector;
using Skill;
using UnityEngine;

namespace Character.BehaviourTree.Node.Action.Condition
{
    [NodeMenuItem("Action/Character/Condition/Random Skill Release")]
    public class CharacterRandomSkillReleaseCondition : ActionNode
    {
        private enum RandomSkillRange
        {
            All,
            Specified,
        }

        private enum RandomSkillTargetType
        {
            All,
            NoTarget,
            HasTarget,
        }

        [SerializeField] private RandomSkillRange range = RandomSkillRange.All;

        [ShowIf("@range == RandomSkillRange.Specified")] [SerializeField]
        private List<string> specifiedSkillIds = new();

        [ShowIf("@range == RandomSkillRange.All")] [SerializeField]
        private RandomSkillTargetType targetType = RandomSkillTargetType.All;

        public override string Description => "角色任意技能释放条件节点，判断当前角色是否满足释放某一技能的条件";

        protected override NodeState OnTick(float deltaTime, object payload)
        {
            if (payload is not CharacterBehaviourTreeParameters parameters || !parameters.SkillManager ||
                !parameters.Character.SkillAbility)
            {
                DebugUtil.LogError("The node can't execute correctly, please check the character ability");
                return NodeState.Failure;
            }

            CharacterObject target = null;
            if (parameters.Shared.TryGetValue(CharacterBehaviourTreeParameters.TargetParams,
                    out var target1))
            {
                target = target1 as CharacterObject;
            }

            switch (range)
            {
                case RandomSkillRange.All:
                {
                    if (targetType == RandomSkillTargetType.HasTarget && !target)
                    {
                        DebugUtil.LogError(
                            $"The target character is not found in the shared dictionary");
                        return NodeState.Failure;
                    }

                    return parameters.Character.SkillAbility.FindAllowReleaseSkills(target, out var skills)
                        ? NodeState.Success
                        : NodeState.Failure;
                }
                    break;
                case RandomSkillRange.Specified:
                {
                    return parameters.Character.SkillAbility.FindAllowReleaseSkills(specifiedSkillIds, target,
                        out var skills)
                        ? NodeState.Success
                        : NodeState.Failure;
                }
                    break;
                default:
                {
                    return NodeState.Failure;
                }
                    break;
            }
        }
    }
}