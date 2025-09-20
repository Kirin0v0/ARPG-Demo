using Character;
using Character.Data;
using Damage.Data;
using UnityEngine;

namespace Damage.SO
{
    public abstract class BaseDamageCriticalRateCalculateSO : ScriptableObject
    {
        public abstract float CalculateCriticalRate(
            DamageType damageType,
            CharacterProperty property
        );
    }
}