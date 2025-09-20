using System;
using Framework.Common.BehaviourTree.Node;
using Framework.Common.BehaviourTree.Node.Action;
using Framework.Common.Debug;
using Sirenix.Utilities;

namespace Character.BehaviourTree.Node.Action.Target
{
    [NodeMenuItem("Action/Character/Target/Warning and Battle/Nearest Enemy")]
    public class CharacterNearestEnemyTargetNode : BaseCharacterTargetNode
    {
        public override string Description => "角色目标节点，如果当前角色处于空闲或无敌人目标返回失败，否则会共享距离自身最近的敌人作为目标角色，并返回成功";

        protected override NodeState OnTick(float deltaTime, object payload)
        {
            if (payload is not CharacterBehaviourTreeParameters parameters || !parameters.Character.BattleAbility)
            {
                DebugUtil.LogError("The node can't execute correctly, please check the character ability");
                return NodeState.Failure;
            }

            if (parameters.Character.Parameters.battleState == CharacterBattleState.Idle)
            {
                return NodeState.Failure;
            }

            var enemies = parameters.Character.Parameters.battleState switch
            {
                CharacterBattleState.Warning => parameters.Character.BattleAbility.DetectedEnemies,
                CharacterBattleState.Battle => parameters.Character.BattleAbility.BattleEnemies,
                _ => Array.Empty<CharacterObject>()
            };
            if (enemies.Length == 0)
            {
                return NodeState.Failure;
            }

            var nearestEnemy = enemies[0];
            enemies.ForEach(enemy =>
            {
                if ((enemy.Parameters.position - parameters.Character.Parameters.position).sqrMagnitude <
                    (nearestEnemy.Parameters.position - parameters.Character.Parameters.position).sqrMagnitude)
                {
                    nearestEnemy = enemy;
                }
            });
            
            ShareTarget(parameters, nearestEnemy);
            
            return NodeState.Success;
        }
    }
}