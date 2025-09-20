using System;
using Animancer;
using Framework.Common.BehaviourTree.Node;
using Framework.Common.BehaviourTree.Node.Action;
using Framework.Common.Debug;
using Framework.Common.Util;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Character.BehaviourTree.Node.Action
{
    [NodeMenuItem("Action/Character/Move To Position")]
    public class CharacterMoveToPositionNode : ActionNode
    {
        [Title("时间配置")] [SerializeField] [MinValue(0.1f)]
        private float duration = 1;

        [Title("动画配置")] [SerializeField] private bool playAnimationWhenMoving = false;

        [SerializeField] [ShowIf("@playAnimationWhenMoving")]
        private bool useStringAsset = false;

        [SerializeField] [ShowIf("@playAnimationWhenMoving && useStringAsset")]
        private StringAsset animationStringAsset;

        [SerializeField] [ShowIf("@playAnimationWhenMoving && !useStringAsset")]
        private TransitionAsset animationTransition;

        private float _time;
        private AnimancerState _animancerState;

        public override string Description =>
            "角色移动节点，使用前请检查先执行的节点是否在运行时数据共享前往地点，每帧会根据时间计算位移，直到计时结束才会返回成功";

        protected override void OnStart(object payload)
        {
            _time = 0f;
            _animancerState = null;
            if (playAnimationWhenMoving)
            {
                if (payload is not CharacterBehaviourTreeParameters parameters ||
                    !parameters.Character.AnimationAbility)
                {
                    DebugUtil.LogError("The node can't execute correctly, please check the character ability");
                    return;
                }

                _animancerState = useStringAsset
                    ? parameters.Character.AnimationAbility.PlayAction(animationStringAsset)
                    : parameters.Character.AnimationAbility.PlayAction(animationTransition);
            }
        }

        protected override void OnResume(object payload)
        {
            if (playAnimationWhenMoving)
            {
                if (payload is not CharacterBehaviourTreeParameters parameters ||
                    !parameters.Character.AnimationAbility)
                {
                    DebugUtil.LogError("The node can't execute correctly, please check the character ability");
                    return;
                }

                _animancerState = useStringAsset
                    ? parameters.Character.AnimationAbility.PlayAction(animationStringAsset)
                    : parameters.Character.AnimationAbility.PlayAction(animationTransition);
            }
        }

        protected override NodeState OnTick(float deltaTime, object payload)
        {
            if (payload is not CharacterBehaviourTreeParameters parameters)
            {
                DebugUtil.LogError("The node can't execute correctly, please check the character ability");
                return NodeState.Failure;
            }

            if (!parameters.Shared.TryGetValue(CharacterBehaviourTreeParameters.GotoParams,
                    out var value) ||
                value is not Vector3 gotoPosition)
            {
                DebugUtil.LogError(
                    $"The goto position is not found in the shared dictionary");
                return NodeState.Failure;
            }

            _time += deltaTime;

            var direction = gotoPosition - parameters.Character.transform.position;
            direction = new Vector3(direction.x, 0, direction.z).normalized;
            parameters.Character.MovementAbility?.Move(direction / duration * deltaTime, true);

            if (_time >= duration)
            {
                return NodeState.Success;
            }

            return NodeState.Running;
        }

        protected override void OnAbort(object payload)
        {
            if (playAnimationWhenMoving && _animancerState != null)
            {
                if (payload is not CharacterBehaviourTreeParameters parameters ||
                    !parameters.Character.AnimationAbility)
                {
                    DebugUtil.LogError("The node can't execute correctly, please check the character ability");
                    return;
                }

                parameters.Character.AnimationAbility.StopAction(_animancerState);
                _animancerState = null;
            }
        }

        protected override void OnStop(object payload)
        {
            if (playAnimationWhenMoving && _animancerState != null)
            {
                if (payload is not CharacterBehaviourTreeParameters parameters ||
                    !parameters.Character.AnimationAbility)
                {
                    DebugUtil.LogError("The node can't execute correctly, please check the character ability");
                    return;
                }

                parameters.Character.AnimationAbility.StopAction(_animancerState);
                _animancerState = null;
            }
        }
    }
}