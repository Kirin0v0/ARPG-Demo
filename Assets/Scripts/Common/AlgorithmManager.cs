using Buff.SO;
using Character;
using Character.Data;
using Character.SO;
using Damage.Data;
using Damage.SO;
using Framework.Common.Debug;
using Framework.Core.Singleton;
using Player.SO;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace Common
{
    public class AlgorithmManager : MonoBehaviour
    {
        [SerializeField, LabelText("角色属性计算攻击力算法")]
        private BaseCharacterAttackAlgorithmSO characterAttackAlgorithmSO;

        public BaseCharacterAttackAlgorithmSO CharacterAttackAlgorithmSO => characterAttackAlgorithmSO;

        [SerializeField, LabelText("角色属性计算防御力算法")]
        private BaseCharacterDefenceAlgorithmSO characterDefenceAlgorithmSO;

        public BaseCharacterDefenceAlgorithmSO CharacterDefenceAlgorithmSO => characterDefenceAlgorithmSO;

        [SerializeField, LabelText("攻击力转伤害算法")]
        private BaseDamageAttackConvertSO damageAttackConvertSO;

        public BaseDamageAttackConvertSO DamageAttackConvertSO => damageAttackConvertSO;

        [SerializeField, LabelText("暴击率算法")] private BaseDamageCriticalRateCalculateSO damageCriticalRateCalculateSO;
        public BaseDamageCriticalRateCalculateSO DamageCriticalRateCalculateSO => damageCriticalRateCalculateSO;

        [SerializeField, LabelText("伤害结算算法")] private BaseDamageSettlementCalculateSO damageSettlementCalculateSO;
        public BaseDamageSettlementCalculateSO DamageSettlementCalculateSO => damageSettlementCalculateSO;
        
        [SerializeField, LabelText("伤害触发机制算法")] private BaseDamageTriggerMechanismSO damageTriggerMechanismSO;
        public BaseDamageTriggerMechanismSO DamageTriggerMechanismSO => damageTriggerMechanismSO;

        [SerializeField, LabelText("玩家等级计算属性算法")]
        private BasePlayerLevelPropertyRuleSO playerLevelPropertyRuleSO;

        public BasePlayerLevelPropertyRuleSO PlayerLevelPropertyRuleSO => playerLevelPropertyRuleSO;

        [SerializeField, LabelText("Buff属性影响算法")]
        private BaseBuffPropertyCalculateSO buffPropertyCalculateSO;

        public BaseBuffPropertyCalculateSO BuffPropertyCalculateSO => buffPropertyCalculateSO;

        [SerializeField, LabelText("Atb累积算法")] private BaseCharacterAtbAccumulateSO atbAccumulateSO;
        public BaseCharacterAtbAccumulateSO AtbAccumulateSO => atbAccumulateSO;

        [SerializeField, LabelText("伤害计算Atb算法")]
        private BaseCharacterAtbConvertSO atbConvertSO;

        public BaseCharacterAtbConvertSO AtbConvertSO => atbConvertSO;

        [FoldoutGroup("测试角色")] [SerializeField]
        private CharacterProperty character;

        [FoldoutGroup("测试伤害")] [SerializeField, LabelText("攻击角色")]
        private CharacterProperty attacker;

        [FoldoutGroup("测试伤害")] [SerializeField, LabelText("伤害固定值")]
        private DamageValue originFixedDamage;

        [FoldoutGroup("测试伤害")] [SerializeField, LabelText("伤害攻击力乘积")]
        private DamageValueMultiplier originDamageTimes;

        [FoldoutGroup("测试伤害")] [SerializeField, LabelText("伤害方式")]
        private DamageMethod damageMethod;

        [FoldoutGroup("测试伤害")] [SerializeField, LabelText("伤害类型")]
        private DamageType damageType;

        [FoldoutGroup("测试伤害")] [SerializeField, LabelText("伤害转化率")]
        private DamageResourceMultiplier resourceMultiplier;

        [FoldoutGroup("测试伤害")] [SerializeField, LabelText("防守角色")]
        private CharacterProperty defender;

        [FoldoutGroup("测试伤害")] [SerializeField, LabelText("防守角色弱点")]
        public DamageValueType weakness;

        [FoldoutGroup("测试伤害")] [SerializeField, LabelText("防守角色免疫")]
        public DamageValueType immunity;

        [FoldoutGroup("测试伤害")] [SerializeField, LabelText("防守角色伤害系数")]
        private float damageMultiplier = 1;

        [FoldoutGroup("测试角色"), Button("计算攻击力")]
        private void CalculateCharacterAttack()
        {
            var result = CharacterAttackAlgorithmSO.CalculateAttack(character);
            DebugUtil.LogGrey($"物理攻击力: {result.physicsAttack}");
            DebugUtil.LogGrey($"魔法攻击力: {result.magicAttack}");
        }

        [FoldoutGroup("测试角色"), Button("计算防御力")]
        private void CalculateCharacterDefence()
        {
            var result = CharacterDefenceAlgorithmSO.CalculateDefence(character);
            DebugUtil.LogGrey($"防御力: {result}");
        }

        [FoldoutGroup("测试角色"), Button("计算暴击率")]
        private void CalculateCharacterCriticalRate()
        {
            var result = DamageCriticalRateCalculateSO.CalculateCriticalRate(DamageType.DirectDamage, character);
            DebugUtil.LogGrey($"暴击率: {result}");
        }

        [FoldoutGroup("测试角色"), Button("计算每秒Atb自然累积量")]
        private void CalculateCharacterAccumulateAtbPerSecond()
        {
            var result = AtbAccumulateSO.AccumulateAtb(character, 1);
            DebugUtil.LogGrey($"每秒Atb自然累积量: {result}");
        }

        [FoldoutGroup("测试伤害"), Button("计算攻击角色预期伤害")]
        private void CalculateAttackerExpectDamage()
        {
            var attackResult = CharacterAttackAlgorithmSO.CalculateAttack(attacker);
            var damageResult = DamageAttackConvertSO.ConvertToDamageValue(
                originFixedDamage,
                originDamageTimes,
                damageType,
                new CharacterParameters
                {
                    property = attacker,
                    physicsAttack = attackResult.physicsAttack,
                    magicAttack = attackResult.magicAttack
                }
            );
            LogDamageValue("攻击角色预期", damageResult);
        }

        [FoldoutGroup("测试伤害"), Button("计算攻击角色对防御角色100次攻击的平均伤害、对应资源以及双方的Atb奖励")]
        private void CalculateAttackerToDefenderAverageDamageAndResourceAndAtb()
        {
            var attackerAttackValue = CharacterAttackAlgorithmSO.CalculateAttack(attacker);
            var defenderDefenceValue = CharacterDefenceAlgorithmSO.CalculateDefence(defender);
            var result = CalculateAttackerToDefenderAverageDamageAndResource(
                attacker.reaction,
                attacker.luck,
                attackerAttackValue.physicsAttack,
                attackerAttackValue.magicAttack,
                damageMethod,
                originFixedDamage,
                originDamageTimes,
                damageType,
                resourceMultiplier,
                defender.reaction,
                defenderDefenceValue,
                damageMultiplier,
                weakness,
                immunity
            );
            LogDamageValue("预期伤害", result.exceptedDamage);
            LogDamageValue("结算伤害", result.finalDamage);
            LogResource("结算资源", result.finalResource);
            DebugUtil.LogGrey($"攻击方奖励Atb: {result.attackerAtbReward}");
            DebugUtil.LogGrey($"防守方奖励Atb: {result.defenderAtbReward}");
        }

        public (DamageValue exceptedDamage,
            DamageValue finalDamage,
            DamageResource finalResource,
            float attackerAtbReward,
            float defenderAtbReward) CalculateAttackerToDefenderAverageDamageAndResource(
                int attackerReaction,
                int attackerLuck,
                int attackerPhysicsAttack,
                int attackerMagicAttack,
                DamageMethod damageMethod,
                DamageValue fixedDamage,
                DamageValueMultiplier damageTimes,
                DamageType damageType,
                DamageResourceMultiplier resourceMultiplier,
                int defenderReaction,
                int defenderDefence,
                float damageMultiplier,
                DamageValueType weakness,
                DamageValueType immunity
            )
        {
            var totalFinalDamageValue = DamageValue.Zero;

            var attackerParameters = new CharacterParameters
            {
                property = new CharacterProperty
                {
                    reaction = attackerReaction,
                    luck = attackerLuck
                },
                physicsAttack = attackerPhysicsAttack,
                magicAttack = attackerMagicAttack
            };
            var defenderParameters = new CharacterParameters
            {
                property = new CharacterProperty
                {
                    reaction = defenderReaction
                },
                defence = defenderDefence,
                damageMultiplier = damageMultiplier,
                weakness = weakness,
                immunity = immunity,
            };
            var expectedDamageValue = DamageAttackConvertSO.ConvertToDamageValue(
                fixedDamage,
                damageTimes,
                damageType,
                attackerParameters
            );
            for (var i = 0; i < 100; i++)
            {
                var criticalRate =
                    DamageCriticalRateCalculateSO.CalculateCriticalRate(damageType, attackerParameters.property);
                var damageSettlement = DamageSettlementCalculateSO.CalculateDamageSettlement(
                    damageType,
                    expectedDamageValue,
                    criticalRate,
                    attackerParameters,
                    defenderParameters
                );
                totalFinalDamageValue += damageSettlement.damageValue;
            }

            var averageFinalDamageValue = totalFinalDamageValue / 100;

            var damageInfo = new DamageInfo
            {
                Method = damageMethod,
                Value = averageFinalDamageValue,
                ResourceMultiplier = resourceMultiplier
            };
            var averageFinalDamageResource =
                DamageInfo.CalculateResource(damageInfo.Value, damageInfo.ResourceMultiplier);
            damageInfo = damageInfo.Settle(damageInfo.Value, averageFinalDamageResource, false);
            var atbRewards =
                AtbConvertSO.ConvertToAtb(damageInfo, attackerParameters.property, defenderParameters.property);
            return (expectedDamageValue, averageFinalDamageValue, averageFinalDamageResource, atbRewards.attackerAtb,
                atbRewards.defenderAtb);
        }

        private void LogDamageValue(string prefix, DamageValue damageValue)
        {
            if (damageValue.noType != 0)
            {
                DebugUtil.LogGrey($"{prefix}无类型伤害: {damageValue.noType}");
            }

            if (damageValue.physics != 0)
            {
                DebugUtil.LogGrey($"{prefix}物理伤害: {damageValue.physics}");
            }

            if (damageValue.fire != 0)
            {
                DebugUtil.LogGrey($"{prefix}火焰伤害: {damageValue.fire}");
            }

            if (damageValue.lightning != 0)
            {
                DebugUtil.LogGrey($"{prefix}雷电伤害: {damageValue.lightning}");
            }

            if (damageValue.ice != 0)
            {
                DebugUtil.LogGrey($"{prefix}冷冻伤害: {damageValue.ice}");
            }

            if (damageValue.wind != 0)
            {
                DebugUtil.LogGrey($"{prefix}风化伤害: {damageValue.wind}");
            }
        }

        private void LogResource(string prefix, DamageResource damageResource)
        {
            if (damageResource.hp != 0)
            {
                DebugUtil.LogGrey($"{prefix}生命值: {damageResource.hp}");
            }

            if (damageResource.mp != 0)
            {
                DebugUtil.LogGrey($"{prefix}魔法值: {damageResource.mp}");
            }

            if (damageResource.stun != 0)
            {
                DebugUtil.LogGrey($"{prefix}硬直值: {damageResource.stun}");
            }

            if (damageResource.@break != 0)
            {
                DebugUtil.LogGrey($"{prefix}破防值: {damageResource.@break}");
            }
        }
    }
}