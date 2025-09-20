using System;
using System.Linq;
using Common;
using Framework.Common.BehaviourTree.Node;
using Framework.Common.BehaviourTree.Node.Action;
using Framework.Common.Debug;
using VContainer;

namespace Character.BehaviourTree.Node.Action.Target
{
    [NodeMenuItem("Action/Character/Target/Battle/Highest Damage Enemy")]
    public class CharacterHighestDamageEnemyTargetNode : BaseCharacterTargetNode
    {
        public override string Description => "战斗角色目标节点，如果当前角色不处于战斗或不存在敌人列表则返回失败，否则会共享对自身造成伤害最高的敌人作为目标角色并返回成功";

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

            var enemyDamageRecords = parameters.Character.BattleAbility.OthersToCharacterDamage
                .Where(pair => pair.Key.Parameters.side != parameters.Character.Parameters.side).ToList();
            if (enemyDamageRecords.Count == 0)
            {
                return NodeState.Failure;
            }

            var highestDamagePair = enemyDamageRecords[0];
            enemyDamageRecords.ForEach(pair =>
            {
                if (pair.Value < highestDamagePair.Value)
                {
                    highestDamagePair = pair;
                }
            });

            ShareTarget(parameters, highestDamagePair.Key);

            return NodeState.Success;
        }
    }
}