using System;
using Character.Data;
using Framework.Common.BehaviourTree.Node;
using Framework.Common.BehaviourTree.Node.Action;
using Framework.Common.Debug;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Character.BehaviourTree.Node.Action.Condition
{
    [NodeMenuItem("Action/Character/Condition/Target Resource")]
    public class CharacterTargetResourceConditionNode : ActionNode
    {
        [SerializeField] private CharacterResource resource;

        public override string Description => "目标角色资源条件节点，使用前请检查是否在该节点执行前执行了目标节点，判断目标角色资源是否满足资源条件，即存在余量";

        protected override NodeState OnTick(float deltaTime, object payload)
        {
            if (payload is not CharacterBehaviourTreeParameters parameters)
            {
                DebugUtil.LogError("The node can't execute correctly");
                return NodeState.Failure;
            }

            if (!parameters.Shared.TryGetValue(CharacterBehaviourTreeParameters.TargetParams,
                    out var target) ||
                target is not CharacterObject targetCharacter)
            {
                DebugUtil.LogError(
                    $"The target character is not found in the shared dictionary");
                return NodeState.Failure;
            }

            return targetCharacter.ResourceAbility.Enough(resource) ? NodeState.Success : NodeState.Failure;
        }
    }
}