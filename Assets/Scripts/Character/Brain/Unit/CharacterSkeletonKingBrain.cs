using Character.BehaviourTree;
using Framework.Common.Blackboard;
using Player;
using UnityEngine;

namespace Character.Brain.Unit
{
    public class CharacterSkeletonKingBrain : CharacterBehaviourTreeBrain
    {
        private float _attackCooldown = 0f;
        private float _postAttackDelay = 0f;
        private float _rageCooldown = 0f;

        private CharacterBattleState _battleState;
        private bool _dead;
        private bool _broken;
        private bool _stunned;
        private bool _defence;

        protected override void OnBrainInit()
        {
            base.OnBrainInit();
            UpdateSkeletonState();
        }

        protected override void OnLogicThoughtsUpdated(float fixedDeltaTime)
        {
            base.OnLogicThoughtsUpdated(fixedDeltaTime);
            UpdateSkeletonState();
        }

        protected override void SetBlackboardBeforeExecuteTick(Blackboard blackboard, float deltaTime)
        {
            _attackCooldown = Mathf.Clamp(_attackCooldown - deltaTime, 0f, _attackCooldown);
            _postAttackDelay = Mathf.Clamp(_postAttackDelay - deltaTime, 0f, _postAttackDelay);
            _rageCooldown = Mathf.Clamp(_rageCooldown - deltaTime, 0f, _rageCooldown);

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
            blackboard.SetFloatParameter("rageCooldown", _rageCooldown);
            blackboard.SetBoolParameter("dead", Owner.Parameters.dead);
            blackboard.SetBoolParameter("broken", Owner.Parameters.broken);
            blackboard.SetBoolParameter("stunned", Owner.Parameters.stunned);
            blackboard.SetBoolParameter("battleStateChanged", Owner.Parameters.battleState != _battleState);
            blackboard.SetBoolParameter("deadStateChanged", Owner.Parameters.dead != _dead);
            blackboard.SetBoolParameter("brokenStateChanged", Owner.Parameters.broken != _broken);
            blackboard.SetBoolParameter("stunnedStateChanged", Owner.Parameters.stunned != _stunned);
            blackboard.SetBoolParameter("defence", Owner.Parameters.inDefence);
            blackboard.SetBoolParameter("playerWantAttack",
                GameManager.Player && (GameManager.Player.PlayerParameters.inAttack ||
                                       GameManager.Player.PlayerParameters.inSkill));
        }

        protected override void GetBlackboardAfterExecuteTick(Blackboard blackboard)
        {
            // 同步行为树内部设置的黑板数据
            _attackCooldown = blackboard.GetFloatParameter("attackCooldown");
            _postAttackDelay = blackboard.GetFloatParameter("postAttackDelay");
            _rageCooldown = blackboard.GetFloatParameter("rageCooldown");
            Owner.Parameters.inDefence = blackboard.GetBoolParameter("defence");
        }

        private void UpdateSkeletonState()
        {
            // 记录当前帧数据
            _battleState = Owner.Parameters.battleState;
            _dead = Owner.Parameters.dead;
            _broken = Owner.Parameters.broken;
            _stunned = Owner.Parameters.stunned;
            // 设置角色伤害系数
            if (Owner.Parameters.inDefence)
            {
                Owner.Parameters.damageMultiplier = Owner.Parameters.defenceDamageMultiplier;
            }
            else
            {
                Owner.Parameters.damageMultiplier = Owner.Parameters.broken
                    ? Owner.Parameters.brokenDamageMultiplier
                    : Owner.Parameters.normalDamageMultiplier;
            }
        }
    }
}