using System;
using Character;
using Character.BehaviourTree;
using Character.Brain;
using Framework.Common.Blackboard;
using Framework.Common.Debug;
using UnityEngine;

namespace Character.Brain.Unit
{
    public class CharacterDragonBrain : CharacterBehaviourTreeBrain
    {
        private CharacterBattleState _battleState;

        protected override void OnBrainInit()
        {
            base.OnBrainInit();
            UpdateDragonState();
        }

        protected override void OnLogicThoughtsUpdated(float fixedDeltaTime)
        {
            base.OnLogicThoughtsUpdated(fixedDeltaTime);
            UpdateDragonState();
        }

        protected override void SetBlackboardBeforeExecuteTick(Blackboard blackboard, float deltaTime)
        {
            var battleStateChanged = false;
            switch (Owner.Parameters.battleState)
            {
                case CharacterBattleState.Idle when _battleState != CharacterBattleState.Idle:
                {
                    battleStateChanged = true;
                }
                    break;
                case CharacterBattleState.Warning when _battleState == CharacterBattleState.Idle:
                {
                    battleStateChanged = true;
                }
                    break;
                case CharacterBattleState.Battle when _battleState == CharacterBattleState.Idle:
                {
                    battleStateChanged = true;
                }
                    break;
            }

            blackboard.SetIntParameter("battleState", Owner.Parameters.battleState switch
            {
                CharacterBattleState.Idle => 0,
                CharacterBattleState.Warning => 1,
                CharacterBattleState.Battle => 2,
                _ => 0,
            });
            blackboard.SetBoolParameter("battleStateChanged", battleStateChanged);
        }

        protected override void GetBlackboardAfterExecuteTick(Blackboard blackboard)
        {
        }

        private void UpdateDragonState()
        {
            _battleState = Owner.Parameters.battleState;
        }
    }
}