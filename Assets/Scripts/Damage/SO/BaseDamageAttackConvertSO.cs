using Character;
using Character.Data;
using Damage.Data;
using UnityEngine;

namespace Damage.SO
{
    public abstract class BaseDamageAttackConvertSO : ScriptableObject
    {
        public abstract DamageValue ConvertToDamageValue(
            DamageValue originFixedDamage,
            DamageValueMultiplier originDamageTimes,
            DamageType damageType,
            CharacterParameters attacker
        );
    }
}