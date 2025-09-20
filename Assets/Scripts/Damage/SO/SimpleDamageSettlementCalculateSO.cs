using Character;
using Character.Data;
using Damage.Data;
using Damage.Data.Extension;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Damage.SO
{
    [CreateAssetMenu(menuName = "Damage/Settlement Calculate/Simple")]
    public class SimpleDamageSettlementCalculateSO : BaseDamageSettlementCalculateSO
    {
        [SerializeField, MinValue(0f)] private float weaknessMultiplier = 2f;
        [SerializeField, MinValue(0f)] private float immunityMultiplier = 0f;
        [SerializeField, MinValue(1f)] private float criticalMultiplier = 1.5f;

        public override (DamageValue damageValue, bool isCritical) CalculateDamageSettlement(
            DamageType type,
            DamageValue value,
            float criticalRate,
            CharacterParameters attacker,
            CharacterParameters defender
        )
        {
            // 治疗则直接无视双方弱点、免疫等，仅计算暴击后数值
            if (type.IsHeal())
            {
                var healCritical = criticalRate != 0f && Random.Range(0.00f, 1.00f) <= criticalRate;
                var healDamageValue = value.Check(true) * (healCritical ? criticalMultiplier : 1.00f);
                return (healDamageValue, healCritical);
            }

            // 计算弱点、免疫值加成伤害
            var damageValue = CalculateDamageWeaknessAndImmunityImpact(value, defender);
            // 计算在受击者的防御减免和伤害系数加成后伤害
            var defenceReduction = 1 - 1f * defender.defence / (defender.defence + 100);
            damageValue *= (defenceReduction * defender.damageMultiplier);
            // 最后再计算暴击伤害
            var damageCritical = criticalRate != 0f && Random.Range(0.00f, 1.00f) <= criticalRate;
            damageValue = damageValue.Check(false) * (damageCritical ? criticalMultiplier : 1.00f);
            return (damageValue, damageCritical);
        }

        /// <summary>
        /// 针对受击者的弱点和免疫值计算伤害加成，这里我们规定如下
        /// 1.如果伤害类型是目标免疫的则乘上免疫系数;
        /// 2.如果伤害类型是目标弱点则乘上弱点系数;
        /// </summary>
        /// <param name="attackDamage"></param>
        /// <param name="defender"></param>
        /// <returns></returns>
        private DamageValue CalculateDamageWeaknessAndImmunityImpact(
            DamageValue attackDamage,
            CharacterParameters defender
        )
        {
            if ((defender.immunity & DamageValueType.Physics) != 0)
            {
                attackDamage.physics = (int)(attackDamage.physics * immunityMultiplier);
            }
            else if ((defender.weakness & DamageValueType.Physics) != 0)
            {
                attackDamage.physics = (int)(attackDamage.physics * weaknessMultiplier);
            }

            if ((defender.immunity & DamageValueType.Fire) != 0)
            {
                attackDamage.fire = (int)(attackDamage.fire * immunityMultiplier);
            }
            else if ((defender.weakness & DamageValueType.Fire) != 0)
            {
                attackDamage.fire = (int)(attackDamage.fire * weaknessMultiplier);
            }

            if ((defender.immunity & DamageValueType.Ice) != 0)
            {
                attackDamage.ice = (int)(attackDamage.ice * immunityMultiplier);
            }
            else if ((defender.weakness & DamageValueType.Ice) != 0)
            {
                attackDamage.ice = (int)(attackDamage.ice * weaknessMultiplier);
            }

            if ((defender.immunity & DamageValueType.Wind) != 0)
            {
                attackDamage.wind = (int)(attackDamage.wind * immunityMultiplier);
            }
            else if ((defender.weakness & DamageValueType.Wind) != 0)
            {
                attackDamage.wind = (int)(attackDamage.wind * weaknessMultiplier);
            }

            if ((defender.immunity & DamageValueType.Lightning) != 0)
            {
                attackDamage.lightning = (int)(attackDamage.lightning * immunityMultiplier);
            }
            else if ((defender.weakness & DamageValueType.Lightning) != 0)
            {
                attackDamage.lightning = (int)(attackDamage.lightning * weaknessMultiplier);
            }

            return attackDamage;
        }
    }
}