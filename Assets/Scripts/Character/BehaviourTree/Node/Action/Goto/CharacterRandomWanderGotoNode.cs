using System;
using Framework.Common.BehaviourTree.Node;
using Framework.Common.BehaviourTree.Node.Action;
using Framework.Common.Debug;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

namespace Character.BehaviourTree.Node.Action.Goto
{
    [NodeMenuItem("Action/Character/Goto/Random Wander")]
    public class CharacterRandomWanderGotoNode : BaseCharacterGotoNode
    {
        private enum WanderCenterType
        {
            SpawnPoint,
            CurrentPosition,
        }

        [SerializeField] private WanderCenterType centerType = WanderCenterType.SpawnPoint;
        [SerializeField] private float radius;

        public override string Description => "角色随机漫游前往节点，用于共享前往数据";

        protected override NodeState OnTick(float deltaTime, object payload)
        {
            if (payload is not CharacterBehaviourTreeParameters parameters)
            {
                DebugUtil.LogError("The node can't execute correctly");
                return NodeState.Failure;
            }

            var offset = radius * Random.insideUnitCircle;
            var position = centerType switch
            {
                WanderCenterType.SpawnPoint => parameters.Character.Parameters.spawnPoint + new Vector3(offset.x, 0f, offset.y),
                WanderCenterType.CurrentPosition => parameters.Character.Parameters.position + new Vector3(offset.x, 0f, offset.y),
                _ => parameters.Character.Parameters.spawnPoint + new Vector3(offset.x, 0f, offset.y),
            };
            ShareGoto(parameters, position);

            return NodeState.Success;
        }
    }
}