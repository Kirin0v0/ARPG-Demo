using Damage.Data;
using Framework.Common.Debug;
using UnityEngine;

namespace Common
{
    public class ConfiguredAlgorithmTest : IAlgorithmTest
    {
        private readonly AlgorithmManager _algorithmManager = GameEnvironment.FindEnvironmentComponent<AlgorithmManager>();

        public void TestDamageAndResourceAndAtb(
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
            if (!_algorithmManager)
            {
                DebugUtil.LogError("Can't find the AlgorithmManager in scene");
                return;
            }

            var result = _algorithmManager.CalculateAttackerToDefenderAverageDamageAndResource(
                attackerReaction,
                attackerLuck,
                attackerPhysicsAttack,
                attackerMagicAttack,
                damageMethod,
                fixedDamage,
                damageTimes,
                damageType,
                resourceMultiplier,
                defenderReaction,
                defenderDefence,
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