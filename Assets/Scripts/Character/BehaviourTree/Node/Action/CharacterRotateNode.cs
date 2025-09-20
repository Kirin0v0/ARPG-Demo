using System;
using Framework.Common.BehaviourTree.Node;
using Framework.Common.BehaviourTree.Node.Action;
using Framework.Common.Debug;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Character.BehaviourTree.Node.Action
{
    [NodeMenuItem("Action/Character/Rotate")]
    public class CharacterRotateNode : ActionNode
    {
        [SerializeField] private float angle;
        [SerializeField] [MinValue(0.1f)] private float duration;

        private float _time;

        public override string Description => "角色旋转节点，每帧会调整角色旋转量，旋转结束才会返回成功";

        protected override void OnStart(object payload)
        {
            _time = 0f;
        }

        protected override NodeState OnTick(float deltaTime, object payload)
        {
            if (payload is not CharacterBehaviourTreeParameters parameters)
            {
                DebugUtil.LogError("The node can't execute correctly, please check the character ability");
                return NodeState.Failure;
            }

            _time += deltaTime;

            parameters.Character.MovementAbility?.Rotate(Quaternion.AngleAxis(deltaTime * angle / duration, Vector3.up),
                true);

            if (_time >= duration)
            {
                return NodeState.Success;
            }

            return NodeState.Running;
        }
    }
}