using Damage.Data;
using UnityEngine;

namespace Character.Ability
{
    public abstract class CharacterStateChangeAbility : BaseCharacterOptionalAbility
    {
        public abstract void OnBeKilled(DamageInfo? damageInfo);

        public abstract void OnBeRespawned(DamageInfo? damageInfo);

        public abstract void OnIntoStunned(DamageInfo? damageInfo);

        public abstract void OnExitStunned(DamageInfo? damageInfo);

        public abstract void OnIntoBroken(DamageInfo? damageInfo);

        public abstract void OnExitBroken(DamageInfo? damageInfo);

        public abstract void OnBattleStateChanged(CharacterBattleState previousState, CharacterBattleState currentState);
    }
}