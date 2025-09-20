using Character;
using Character.Data;
using Damage.Data;
using Damage.Data.Extension;
using UnityEngine;

namespace Damage.SO
{
    [CreateAssetMenu(menuName = "Damage/Critical Rate Calculate/Simple")]
    public class SimpleDamageCriticalRateCalculateSo : BaseDamageCriticalRateCalculateSO
    {
        public override float CalculateCriticalRate(DamageType damageType, CharacterProperty property)
        {
            if (!damageType.AllowCritical())
            {
                return 0f;
            }

            return 1f * property.luck / (property.luck + 100);
        }
    }
}