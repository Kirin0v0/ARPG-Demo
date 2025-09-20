using System;
using Character.Ability;
using Framework.Common.BehaviourTree.Node;
using Framework.Common.BehaviourTree.Node.Action;
using Framework.Common.Debug;
using UnityEngine;

namespace Character.BehaviourTree.Node.Action
{
    [NodeMenuItem("Action/Character/Clear Animations")]
    public class CharacterClearAnimationsNode : ActionNode
    {
        public override string Description => "角色清除动画节点，通知角色清除动画，并立即返回成功";

        protected override void OnStart(object payload)
        {
            if (payload is not CharacterBehaviourTreeParameters parameters || !parameters.Character.AnimationAbility)
            {
                DebugUtil.LogError("The node can't execute correctly, please check the character ability");
                return;
            }
            
            parameters.Character.AnimationAbility.ClearAllLayers();
        }
        
        protected override NodeState OnTick(float deltaTime, object payload)
        {
            return NodeState.Success;
        }
    }
}