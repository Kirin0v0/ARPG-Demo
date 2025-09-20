using Damage.Data;
using UnityEngine;

namespace Damage.SO
{
    public abstract class BaseDamageTriggerMechanismSO: ScriptableObject
    {
        public abstract int GetTriggerFlags(DamageInfo damageInfo);
    }
}