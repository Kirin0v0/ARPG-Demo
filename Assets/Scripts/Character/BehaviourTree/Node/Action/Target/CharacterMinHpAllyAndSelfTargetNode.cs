using System;
using System.Linq;
using Framework.Common.BehaviourTree.Node;
using Framework.Common.BehaviourTree.Node.Action;
using Framework.Common.Debug;
using Sirenix.Utilities;

namespace Character.BehaviourTree.Node.Action.Target
{
    [NodeMenuItem("Action/Character/Target/Battle/Min Hp Ally&Self")]
    public class CharacterMinHpAllyAndSelfTargetNode : BaseCharacterTargetNode
    {
        public override string Description => "角色目标节点，如果当前角色不处于战斗返回失败，否则会共享友军及自身中Hp最低的角色作为目标角色，并返回成功";

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

            if (parameters.Character.BattleAbility.BattleAllies.Length == 0)
            {
                if (parameters.Shared.ContainsKey(CharacterBehaviourTreeParameters.TargetParams))
                {
                    parameters.Shared[CharacterBehaviourTreeParameters.TargetParams] = parameters.Character;
                }
                else
                {
                    parameters.Shared.Add(CharacterBehaviourTreeParameters.TargetParams, parameters.Character);
                }

                return NodeState.Success;
            }

            var minHpCharacter = parameters.Character;
            parameters.Character.BattleAbility.BattleAllies.ForEach(ally =>
            {
                if (ally.Parameters.resource.hp < minHpCharacter.Parameters.resource.hp)
                {
                    minHpCharacter = ally;
                }
            });

            ShareTarget(parameters, minHpCharacter);

            return NodeState.Success;
        }
    }
}