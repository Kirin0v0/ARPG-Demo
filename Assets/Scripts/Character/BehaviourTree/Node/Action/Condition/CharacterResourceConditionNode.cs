using System;
using Character.Data;
using Framework.Common.BehaviourTree.Node;
using Framework.Common.BehaviourTree.Node.Action;
using Framework.Common.Debug;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Character.BehaviourTree.Node.Action.Condition
{
    [NodeMenuItem("Action/Character/Condition/Resource")]
    public class CharacterResourceConditionNode : ActionNode
    {
        [SerializeField] private CharacterResource resource;

        public override string Description => "角色资源条件节点，判断当前角色资源是否满足资源条件，即存在余量";

        protected override NodeState OnTick(float deltaTime, object payload)
        {
            if (payload is not CharacterBehaviourTreeParameters parameters)
            {
                DebugUtil.LogError("The node can't execute correctly");
                return NodeState.Failure;
            }

            return parameters.Character.ResourceAbility.Enough(resource) ? NodeState.Success : NodeState.Failure;
        }
    }
}