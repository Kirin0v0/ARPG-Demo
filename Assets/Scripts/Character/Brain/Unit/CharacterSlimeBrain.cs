using System;
using Character;
using Character.BehaviourTree;
using Character.Brain;
using Framework.Common.Blackboard;
using Framework.Common.Debug;
using UnityEngine;

namespace Character.Brain.Unit
{
    public class CharacterSlimeBrain : CharacterBehaviourTreeBrain
    {
        private float _attackCooldown = 0f;
        private float _postAttackDelay = 0f;

        private CharacterBattleState _battleState;
        private bool _dead;
        private bool _broken;
        private bool _stunned;

        protected override void OnBrainInit()
        {
            base.OnBrainInit();
            UpdateSlimeState();
        }

        protected override void OnLogicThoughtsUpdated(float fixedDeltaTime)
        {
            base.OnLogicThoughtsUpdated(fixedDeltaTime);
            UpdateSlimeState();
        }

        protected override void SetBlackboardBeforeExecuteTick(Blackboard blackboard, float deltaTime)
        {
            _attackCooldown = Mathf.Clamp(_attackCooldown - deltaTime, 0f, _attackCooldown);
            _postAttackDelay = Mathf.Clamp(_postAttackDelay - deltaTime, 0f, _postAttackDelay);
            blackboard.SetIntParameter("battleState", Owner.Parameters.battleState switch
            {
                CharacterBattleState.Idle => 0,
                CharacterBattleState.Warning => 1,
                CharacterBattleState.Battle => 2,
                _ => 0,
            });
            blackboard.SetBoolParameter("hasTarget", Shared.ContainsKey(CharacterBehaviourTreeParameters.TargetParams));
            blackboard.SetFloatParameter("attackCooldown", _attackCooldown);
            blackboard.SetFloatParameter("postAttackDelay", _postAttackDelay);
            blackboard.SetBoolParameter("dead", Owner.Parameters.dead);
            blackboard.SetBoolParameter("broken", Owner.Parameters.broken);
            blackboard.SetBoolParameter("stunned", Owner.Parameters.stunned);
            blackboard.SetBoolParameter("battleStateChanged", Owner.Parameters.battleState != _battleState);
            blackboard.SetBoolParameter("deadStateChanged", Owner.Parameters.dead != _dead);
            blackboard.SetBoolParameter("brokenStateChanged", Owner.Parameters.broken != _broken);
            blackboard.SetBoolParameter("stunnedStateChanged", Owner.Parameters.stunned != _stunned);
        }

        protected override void GetBlackboardAfterExecuteTick(Blackboard blackboard)
        {
            // 同步行为树内部设置的黑板数据
            _attackCooldown = blackboard.GetFloatParameter("attackCooldown");
            _postAttackDelay = blackboard.GetFloatParameter("postAttackDelay");
        }

        private void UpdateSlimeState()
        {
            _battleState = Owner.Parameters.battleState;
            _dead = Owner.Parameters.dead;
            _broken = Owner.Parameters.broken;
            _stunned = Owner.Parameters.stunned;
        }
    }
}