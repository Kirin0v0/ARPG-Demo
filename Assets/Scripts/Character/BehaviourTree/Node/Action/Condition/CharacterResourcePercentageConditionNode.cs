using System;
using Character.Data;
using Framework.Common.BehaviourTree.Node;
using Framework.Common.BehaviourTree.Node.Action;
using Framework.Common.Debug;
using UnityEngine;

namespace Character.BehaviourTree.Node.Action.Condition
{
    [NodeMenuItem("Action/Character/Condition/Resource Percentage")]
    public class CharacterResourcePercentageConditionNode : ActionNode
    {
        [SerializeField] [Range(0, 1)] private float maxHpPercentage; // 最大hp百分比
        [SerializeField] [Range(0, 1)] private float maxMpPercentage; // 最大mp百分比
        [SerializeField] [Range(0, 1)] private float stunMeterPercentage; // 硬直量表百分比
        [SerializeField] [Range(0, 1)] private float breakMeterPercentage; // 破防量表百分比

        public override string Description => "角色资源百分比条件节点，判断当前角色资源是否满足资源百分比对应的条件，即存在余量";

        protected override NodeState OnTick(float deltaTime, object payload)
        {
            if (payload is not CharacterBehaviourTreeParameters parameters)
            {
                DebugUtil.LogError("The node can't execute correctly");
                return NodeState.Failure;
            }

            var resource = new CharacterResource
            {
                hp = (int)(parameters.Character.Parameters.property.maxHp * maxHpPercentage),
                mp = (int)(parameters.Character.Parameters.property.maxMp * maxMpPercentage),
                stun = parameters.Character.Parameters.property.stunMeter * stunMeterPercentage,
                @break = parameters.Character.Parameters.property.breakMeter * breakMeterPercentage,
            };
            return parameters.Character.ResourceAbility.Enough(resource) ? NodeState.Success : NodeState.Failure;
        }
    }
}