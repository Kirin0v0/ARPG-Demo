using Character;
using Damage.Data;
using UnityEngine;

namespace Damage.SO
{
    public abstract class BaseDamageSettlementCalculateSO : ScriptableObject
    {
        public abstract (DamageValue damageValue, bool isCritical) CalculateDamageSettlement(
            DamageType type,
            DamageValue value,
            float criticalRate,
            CharacterParameters attacker,
            CharacterParameters defender
        );
    }
}