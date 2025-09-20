using System;
using Framework.Common.BehaviourTree.Node;
using Framework.Common.BehaviourTree.Node.Action;
using Framework.Common.Debug;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Character.BehaviourTree.Node.Action
{
    [NodeMenuItem("Action/Character/Move By Damage")]
    public class CharacterMoveByDamageNode : ActionNode
    {
        private enum DamageType
        {
            Stunned,
            Broken,
            Dead,
        }

        [SerializeField] private DamageType damageType;
        [SerializeField] private float distance;
        [SerializeField] [MinValue(0f)] private float duration = 0;

        public override string Description =>
            "角色伤害受迫移动节点，给角色添加持续移动任务，具体方向视伤害方向而定，但不会阻塞节点（即当前帧立即返回成功）";

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

            var damageInfo = damageType switch
            {
                DamageType.Stunned => parameters.Character.StateAbility.CausedIntoStunnedDamageInfo,
                DamageType.Broken => parameters.Character.StateAbility.CausedIntoBrokenDamageInfo,
                DamageType.Dead => parameters.Character.StateAbility.CausedDeadDamageInfo,
                _ => null
            };
            if (damageInfo == null)
            {
                return NodeState.Success;
            }
            
            var worldMovement = damageInfo.Value.Direction.normalized * distance;
            if (duration > 0)
            {
                parameters.Character.MovementAbility?.ContinuousMove(duration, worldMovement, false);
            }
            else
            {
                parameters.Character.MovementAbility?.Move(worldMovement, false);
            }

            return NodeState.Success;
        }
    }
}