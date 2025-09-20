using System;
using Animancer;
using Framework.Common.BehaviourTree.Node;
using Framework.Common.BehaviourTree.Node.Action;
using Framework.Common.Debug;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Character.BehaviourTree.Node.Action
{
    [NodeMenuItem("Action/Character/Animation")]
    public class CharacterAnimationNode : ActionNode
    {
        [SerializeField] private bool useStringAsset = false;

        [SerializeField] [ShowIf("@useStringAsset")]
        private StringAsset animationStringAsset;

        [SerializeField] [ShowIf("@!useStringAsset")]
        private TransitionAsset animationTransition;

        [SerializeField] private float maxDurationIfLoop = 5f;

        private AnimancerState _animancerState;

        public override string Description => "角色动画节点，控制动画的展示和隐藏，动画播放时返回运行中，动画播放结束返回成功";

        protected override void OnStart(object payload)
        {
            _animancerState = null;
            if (payload is CharacterBehaviourTreeParameters parameters && parameters.Character.AnimationAbility)
            {
                _animancerState = useStringAsset
                    ? parameters.Character.AnimationAbility.PlayAction(animationStringAsset)
                    : parameters.Character.AnimationAbility.PlayAction(animationTransition);
            }
        }

        protected override void OnResume(object payload)
        {
            _animancerState = null;
            if (payload is CharacterBehaviourTreeParameters parameters && parameters.Character.AnimationAbility)
            {
                _animancerState = useStringAsset
                    ? parameters.Character.AnimationAbility.PlayAction(animationStringAsset)
                    : parameters.Character.AnimationAbility.PlayAction(animationTransition);
            }
        }

        protected override NodeState OnTick(float deltaTime, object payload)
        {
            if (_animancerState == null)
            {
                DebugUtil.LogError("The animation can't play");
                return NodeState.Failure;
            }

            return _animancerState.IsLooping
                ? (_animancerState.Time >= maxDurationIfLoop ? NodeState.Success : NodeState.Running)
                : (_animancerState.NormalizedTime >= 1 ? NodeState.Success : NodeState.Running);
        }

        protected override void OnAbort(object payload)
        {
            if (payload is CharacterBehaviourTreeParameters parameters && parameters.Character.AnimationAbility &&
                _animancerState != null)
            {
                parameters.Character.AnimationAbility.StopAction(_animancerState);
            }

            _animancerState = null;
        }

        protected override void OnStop(object payload)
        {
            if (payload is CharacterBehaviourTreeParameters parameters && parameters.Character.AnimationAbility &&
                _animancerState != null)
            {
                parameters.Character.AnimationAbility.StopAction(_animancerState);
            }

            _animancerState = null;
        }
    }
}