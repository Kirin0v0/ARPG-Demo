using System.Collections.Generic;
using Character.Data;
using Damage.Data;
using Damage.Data.Extension;
using Framework.Common.Debug;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Character.SO
{
    [CreateAssetMenu(menuName = "Character/Atb Convert/Simple")]
    public class SimpleCharacterAtbConverSO : BaseCharacterAtbConvertSO
    {
        [Title("伤害奖励Atb基本数值")] [SerializeField, Min(0f)]
        private float damageRewardBaseNumber = 10;

        [SerializeField, Min(0f)] private float damageRewardMultiplier = 0.002f;

        [Title("连招武器乘区")] [SerializeField]
        private Dictionary<DamageComboWeaponType, float> comboWeaponTypeMultipliers = new();

        [Title("反应属性乘区")] [SerializeField, Min(0f)]
        private float reactionRewardBaseNumber = 4;

        public override (float attackerAtb, float defenderAtb) ConvertToAtb(
            DamageInfo damageInfo,
            CharacterProperty attacker,
            CharacterProperty defender
        )
        {
            // 这里规定不奖励Atb的场景：治疗、Buff方式的伤害、环境方式的伤害
            if (damageInfo.Type.IsHeal() || damageInfo.Method is DamageBuffMethod ||
                damageInfo.Method is DamageEnvironmentMethod)
            {
                return (0, 0);
            }

            // 根据Hp资源值计算本次攻击奖励的Atb基本量
            var totalDamage = damageInfo.Resource.hp;
            var atbReward = totalDamage > 0
                ? (totalDamage <= damageRewardBaseNumber
                    ? 1
                    : Mathf.Log(totalDamage, damageRewardBaseNumber)) * damageRewardMultiplier
                : 0;

            // 返回双方的奖励Atb
            return (CalculateAttackerRewardAtb(atbReward, damageInfo, attacker),
                CalculateDefenderRewardAtb(atbReward, damageInfo, defender));
        }

        /// <summary>
        /// 计算攻击方奖励Atb，攻击方享受自身奖励系数、伤害方式以及自身反应属性加成
        /// </summary>
        /// <param name="originAtb"></param>
        /// <returns></returns>
        private float CalculateAttackerRewardAtb(float originAtb, DamageInfo damageInfo, CharacterProperty property)
        {
            var atbReward = originAtb;
            // 先计算自身奖励系数
            atbReward *= damageInfo.ResourceMultiplier.atb;
            // 根据伤害方式计算Atb加成，如果是连招则额外享受武器类型加成，方便不同武器类型做出Atb奖励区分
            if (damageInfo.Method is DamageComboMethod comboMethod &&
                comboWeaponTypeMultipliers.TryGetValue(comboMethod.WeaponType, out var multiplier))
            {
                atbReward *= multiplier;
            }

            // 最后才是根据自身反应属性计算加成
            atbReward *= property.reaction <= reactionRewardBaseNumber
                ? 1
                : Mathf.Log(property.reaction, reactionRewardBaseNumber);
            return atbReward;
        }

        /// <summary>
        /// 计算防守方奖励Atb，防守方仅享受自身反应属性加成
        /// </summary>
        /// <param name="originAtb"></param>
        /// <returns></returns>
        private float CalculateDefenderRewardAtb(float originAtb, DamageInfo damageInfo, CharacterProperty property)
        {
            var atbReward = originAtb;
            // 仅根据自身反应属性计算加成
            atbReward *= property.reaction <= reactionRewardBaseNumber
                ? 1
                : Mathf.Log(property.reaction, reactionRewardBaseNumber);
            return atbReward;
        }
    }
}