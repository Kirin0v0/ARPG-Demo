using Framework.Common.BehaviourTree.Node;
using Framework.Common.BehaviourTree.Node.Action;
using Framework.Common.Debug;
using UnityEngine;

namespace Character.BehaviourTree.Node.Action.Condition
{
    [NodeMenuItem("Action/Character/Condition/Buff Exist")]
    public class CharacterBuffExistConditionNode : ActionNode
    {
        [SerializeField] private string buffId;

        public override string Description => "角色Buff存在条件节点，如果角色身上存在该Buff则返回成功，否则返回失败";

        protected override NodeState OnTick(float deltaTime, object payload)
        {
            if (payload is not CharacterBehaviourTreeParameters parameters || !parameters.Character.BuffAbility)
            {
                DebugUtil.LogError("The node can't execute correctly, please check the ability");
                return NodeState.Failure;
            }

            var buffs = parameters.Character.BuffAbility.GetBuffs(buffId);
            return buffs.Count != 0 ? NodeState.Success : NodeState.Failure;
        }
    }
}