using System;
using Animancer;
using Framework.Common.BehaviourTree.Node;
using Framework.Common.BehaviourTree.Node.Action;
using Framework.Common.Debug;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Character.BehaviourTree.Node.Action
{
    [NodeMenuItem("Action/Character/Switch Animation")]
    public class CharacterSwitchAnimationNode : ActionNode
    {
        [SerializeField] private bool useStringAsset = false;

        [SerializeField] [ShowIf("@useStringAsset")]
        private StringAsset animationStringAsset;

        [SerializeField] [ShowIf("@!useStringAsset")]
        private TransitionAsset animationTransition;

        [SerializeField] private bool resetTime = true;

        [SerializeField] private bool clearAllAnimations = true;

        public override string Description => "角色切换动画节点，仅通知角色切换动画，不控制后续的隐藏，立刻返回播放成功";

        protected override void OnStart(object payload)
        {
            if (payload is not CharacterBehaviourTreeParameters parameters || !parameters.Character.AnimationAbility)
            {
                DebugUtil.LogError("The node can't execute correctly, please check the character ability");
                return;
            }

            if (clearAllAnimations)
            {
                parameters.Character.AnimationAbility.ClearAllLayers();
            }

            if (useStringAsset)
            {
                parameters.Character.AnimationAbility.SwitchBase(animationStringAsset, resetTime);
            }
            else
            {
                parameters.Character.AnimationAbility.SwitchBase(animationTransition, resetTime);
            }
        }

        protected override NodeState OnTick(float deltaTime, object payload) => NodeState.Success;
    }
}