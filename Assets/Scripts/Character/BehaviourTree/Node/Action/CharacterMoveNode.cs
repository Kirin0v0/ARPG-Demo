using System;
using Framework.Common.BehaviourTree.Node;
using Framework.Common.BehaviourTree.Node.Action;
using Framework.Common.Debug;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Character.BehaviourTree.Node.Action
{
    [NodeMenuItem("Action/Character/Move")]
    public class CharacterMoveNode : ActionNode
    {
        [SerializeField] private Vector3 movement;
        [SerializeField] private bool proactive;
        [SerializeField] [MinValue(0f)] private float duration = 0;

        public override string Description =>
            "角色移动节点，给角色添加持续移动任务，但不会阻塞节点（即当前帧立即返回成功）";

        protected override void OnStart(object payload)
        {
        }

        protected override NodeState OnTick(float deltaTime, object payload)
        {
            if (payload is not CharacterBehaviourTreeParameters parameters)
            {
                DebugUtil.LogError("The node can't execute correctly, please check the character ability");
                return NodeState.Failure;
            }

            var worldMovement = parameters.Character.transform.TransformDirection(movement);
            if (duration > 0)
            {
                parameters.Character.MovementAbility?.ContinuousMove(duration, worldMovement, proactive);
            }
            else
            {
                parameters.Character.MovementAbility?.Move(worldMovement, proactive);
            }

            return NodeState.Success;
        }
    }
}