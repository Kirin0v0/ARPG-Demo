using System;
using Framework.Common.BehaviourTree.Node;
using Framework.Common.BehaviourTree.Node.Action;
using Framework.Common.Debug;
using UnityEngine;

namespace Character.BehaviourTree.Node.Action.Compare
{
    [NodeMenuItem("Action/Character/Compare/Target is Behind")]
    public class CharacterTargetIsBehindCompareNode: ActionNode
    {
        public override string Description =>
            "角色比较节点，使用前请检查先执行的节点是否在运行时数据共享目标角色或目标地点，比较目标角色是否处于当前角色面前，在前面返回成功，在后面则返回失败";

        protected override NodeState OnTick(float deltaTime, object payload)
        {
            if (payload is not CharacterBehaviourTreeParameters parameters)
            {
                DebugUtil.LogError("The node can't execute correctly, please check the character ability");
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

            var direction = targetCharacter.transform.position - parameters.Character.transform.position;
            return Vector3.Dot(direction, parameters.Character.transform.forward) > 0
            ? NodeState.Success
            : NodeState.Failure;
        }
    }
}