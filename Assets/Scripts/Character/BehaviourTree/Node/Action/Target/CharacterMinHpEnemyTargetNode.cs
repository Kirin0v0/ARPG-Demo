using System;
using System.Linq;
using Common;
using Framework.Common.BehaviourTree.Node;
using Framework.Common.BehaviourTree.Node.Action;
using Framework.Common.Debug;
using Sirenix.Utilities;
using VContainer;

namespace Character.BehaviourTree.Node.Action.Target
{
    [NodeMenuItem("Action/Character/Target/Battle/Min Hp Enemy")]
    public class CharacterMinHpEnemyTargetNode : BaseCharacterTargetNode
    {
        public override string Description => "战斗角色目标节点，如果当前角色不处于战斗或不存在敌人列表则返回失败，否则会共享Hp最低的敌人作为目标角色并返回成功";

        protected override NodeState OnTick(float deltaTime, object payload)
        {
            if (payload is not CharacterBehaviourTreeParameters parameters || !parameters.Character.BattleAbility)
            {
                DebugUtil.LogError("The node can't execute correctly, please check the character ability");
                return NodeState.Failure;
            }

            if (parameters.Character.Parameters.battleState != CharacterBattleState.Battle)
            {
                return NodeState.Failure;
            }

            var enemies = parameters.Character.BattleAbility.BattleEnemies;
            if (enemies.Length == 0)
            {
                return NodeState.Failure;
            }

            var minHpEnemy = enemies[0];
            enemies.ForEach(character =>
            {
                if (character.Parameters.resource.hp < minHpEnemy.Parameters.resource.hp)
                {
                    minHpEnemy = character;
                }
            });

            ShareTarget(parameters, minHpEnemy);

            return NodeState.Success;
        }
    }
}