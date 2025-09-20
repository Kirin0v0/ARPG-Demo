using Framework.Common.BehaviourTree.Node;
using Framework.Common.BehaviourTree.Node.Action;
using Framework.Common.Debug;
using Skill;
using UnityEngine;

namespace Character.BehaviourTree.Node.Action.Condition
{
    [NodeMenuItem("Action/Character/Condition/Skill Release")]
    public class CharacterSkillReleaseCondition : ActionNode
    {
        [SerializeField] private string skillId;

        public override string Description => "角色技能释放条件节点，判断当前角色是否满足释放技能的条件";

        protected override NodeState OnTick(float deltaTime, object payload)
        {
            if (payload is not CharacterBehaviourTreeParameters parameters || !parameters.SkillManager)
            {
                DebugUtil.LogError("The node can't execute correctly, please check the parameter");
                return NodeState.Failure;
            }

            return parameters.Character.SkillAbility.MatchSkillPreconditions(skillId, out _)
                ? NodeState.Success
                : NodeState.Failure;
        }
    }
}