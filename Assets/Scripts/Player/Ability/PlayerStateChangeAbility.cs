using Character;
using Character.Ability;
using Common;
using Damage.Data;
using Framework.Common.Debug;
using Player.StateMachine.Action;
using Player.StateMachine.Dead;
using Player.StateMachine.Defence;
using Player.StateMachine.Hit;
using UnityEngine;

namespace Player.Ability
{
    public class PlayerStateChangeAbility : CharacterStateChangeAbility
    {
        private new PlayerCharacterObject Owner => base.Owner as PlayerCharacterObject;
        
        [SerializeField] private PlayerDeadGroundState deadGroundState;
        [SerializeField] private PlayerDeadAirborneState deadAirborneState;
        [SerializeField] private PlayerActionGetupState getupState;
        [SerializeField] private PlayerHitGroundState hitGroundState;
        [SerializeField] private PlayerHitAirborneState hitAirborneState;
        [SerializeField] private PlayerHitDefenceBreakState hitDefenceBreakState;

        public override void OnBeKilled(DamageInfo? damageInfo)
        {
            // 中断技能释放
            Owner.SkillAbility?.StopAllReleasingSkills();
            
            // 如果处于空中受击状态，可以暂时忽略，在该状态退出时会自动判断当前是否死亡
            if (Owner.StateMachine.CurrentState is PlayerHitAirborneState)
            {
                return;
            }

            // 默认直接切换到死亡状态
            if (Owner.Parameters.Airborne)
            {
                Owner.StateMachine.SwitchState(deadAirborneState, true);
            }
            else
            {
                Owner.StateMachine.SwitchState(deadGroundState, true);
            }
        }

        public override void OnBeRespawned(DamageInfo? damageInfo)
        {
            Owner.StateMachine.SwitchState(getupState, true);
        }

        public override void OnIntoStunned(DamageInfo? damageInfo)
        {
            // 如果此时角色处于死亡或复活状态，则不打断状态
            if (Owner.StateMachine.CurrentState is PlayerDeadState ||
                Owner.StateMachine.CurrentState is PlayerActionGetupState)
            {
                return;
            }

            // 切换到各种受击状态
            if (Owner.StateMachine.CurrentState is PlayerDefenceState)
            {
                if (Owner.StateMachine.SwitchState(hitDefenceBreakState, true))
                {
                    return;
                }
            }

            if (Owner.Parameters.Airborne)
            {
                Owner.StateMachine.SwitchState(hitAirborneState, true);
            }
            else
            {
                Owner.StateMachine.SwitchState(hitGroundState, true);
            }
        }

        public override void OnExitStunned(DamageInfo? damageInfo)
        {
            // 如果此时角色处于死亡或复活状态，则不打断状态
            if (Owner.StateMachine.CurrentState is PlayerDeadState ||
                Owner.StateMachine.CurrentState is PlayerActionGetupState)
            {
                return;
            }

            // 从受击状态切换回默认状态
            if (Owner.StateMachine.CurrentState is PlayerHitState)
            {
                Owner.StateMachine.SwitchToDefault();
            }
        }

        public override void OnIntoBroken(DamageInfo? damageInfo)
        {
        }

        public override void OnExitBroken(DamageInfo? damageInfo)
        {
        }

        public override void OnBattleStateChanged(CharacterBattleState previousState, CharacterBattleState currentState)
        {
        }
    }
}