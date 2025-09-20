using System;
using Character.Data;
using Framework.Common.BehaviourTree.Node;
using Framework.Common.BehaviourTree.Node.Action;
using Framework.Common.Debug;
using UnityEngine;

namespace Character.BehaviourTree.Node.Action.Condition
{
    [NodeMenuItem("Action/Character/Condition/Target Resource Percentage")]
    public class CharacterTargetResourcePercentageConditionNode : ActionNode
    {
        [SerializeField] [Range(0, 1)] private float maxHpPercentage; // 最大hp百分比
        [SerializeField] [Range(0, 1)] private float maxMpPercentage; // 最大mp百分比
        [SerializeField] [Range(0, 1)] private float stunMeterPercentage; // 硬直量表百分比
        [SerializeField] [Range(0, 1)] private float breakMeterPercentage; // 破防量表百分比

        public override string Description => "目标角色资源条件节点，使用前请检查是否在该节点执行前执行了目标节点，判断目标角色资源是否满足资源百分比对应的条件，即存在余量";

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

            var resource = new CharacterResource
            {
                hp = (int)(targetCharacter.Parameters.property.maxHp * maxHpPercentage),
                mp = (int)(targetCharacter.Parameters.property.maxMp * maxMpPercentage),
                stun = targetCharacter.Parameters.property.stunMeter * stunMeterPercentage,
                @break = targetCharacter.Parameters.property.breakMeter * breakMeterPercentage,
            };
            return targetCharacter.ResourceAbility.Enough(resource) ? NodeState.Success : NodeState.Failure;
        }
    }
}