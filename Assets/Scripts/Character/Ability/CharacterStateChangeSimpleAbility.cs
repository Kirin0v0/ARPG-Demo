using Animancer;
using Common;
using Damage.Data;
using Framework.Common.Audio;
using Sirenix.OdinInspector;
using UnityEngine;
using VContainer;

namespace Character.Ability
{
    public class CharacterStateChangeSimpleAbility : CharacterStateChangeAbility
    {
        public override void OnBeKilled(DamageInfo? damageInfo)
        {
            // 中断技能释放
            Owner.SkillAbility?.StopAllReleasingSkills();
        }

        public override void OnBeRespawned(DamageInfo? damageInfo)
        {
        }

        public override void OnIntoStunned(DamageInfo? damageInfo)
        {
        }

        public override void OnExitStunned(DamageInfo? damageInfo)
        {
        }

        public override void OnIntoBroken(DamageInfo? damageInfo)
        {
            // 设置破防伤害系数
            Owner.Parameters.damageMultiplier = Owner.Parameters.brokenDamageMultiplier;
            // 中断技能释放
            Owner.SkillAbility?.StopAllReleasingSkills();
        }

        public override void OnExitBroken(DamageInfo? damageInfo)
        {
            // 恢复正常伤害系数
            Owner.Parameters.damageMultiplier = Owner.Parameters.inDefence
                ? Owner.Parameters.defenceDamageMultiplier
                : Owner.Parameters.normalDamageMultiplier;
        }

        public override void OnBattleStateChanged(CharacterBattleState previousState, CharacterBattleState currentState)
        {
        }
    }
}