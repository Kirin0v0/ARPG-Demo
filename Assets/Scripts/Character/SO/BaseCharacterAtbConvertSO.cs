using Character.Data;
using Damage.Data;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Character.SO
{
    public abstract class BaseCharacterAtbConvertSO : SerializedScriptableObject
    {
        public abstract (float attackerAtb, float defenderAtb) ConvertToAtb(
            DamageInfo damageInfo,
            CharacterProperty attacker,
            CharacterProperty defender
        );
    }
}