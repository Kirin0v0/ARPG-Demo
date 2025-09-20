using System;
using System.Collections.Generic;
using Common;
using Damage;
using Damage.Data;
using Damage.Data.Extension;
using Sirenix.OdinInspector;
using UnityEngine;
using VContainer;

namespace Buff.Config.Logic.Tick
{
    [Serializable]
    public class BuffTickDamageLogic : BaseBuffTickLogic
    {
        [Serializable]
        public class BuffStackDamageTimes
        {
            public int minStack;
            public int maxStack;
            public float damageTimes;
        }

        [Title("伤害数值配置")] [InfoBox("固定伤害数值，和角色属性不挂钩，数值都不得小于0")] [SerializeField]
        private DamageValue fixedDamage = DamageValue.Zero;

        [InfoBox("攻击力伤害系数，和角色属性挂钩，数值都不得小于0")] [SerializeField]
        private DamageValueMultiplier damageMultiplier = DamageValueMultiplier.Zero;

        [InfoBox("Buff层数是否影响最终伤害，默认不影响，全部层数造成的伤害一致，影响则需要额外配置影响规则")] [SerializeField]
        private bool stackImpactToDamage = false;

        [InfoBox("Buff层数对最终伤害的影响（乘算影响）规则：配置的层数会乘算计算伤害，未配置的层数视为不造成伤害")] [ShowIf("stackImpactToDamage")] [SerializeField]
        private List<BuffStackDamageTimes> stackDamageTimes = new();

        [InfoBox("影响是否暴击的因素之一")] [SerializeField]
        private bool allowCalculateCriticalRate = false;

        [InfoBox("最终伤害转资源系数，数值都不得小于0")] [SerializeField]
        private DamageResourceMultiplier resourceMultiplier = DamageResourceMultiplier.Hp;

        [Title("伤害类型配置")] [InfoBox("伤害类型，注意普通伤害、真实伤害和普通治疗的区分")] [SerializeField]
        private DamageType damageType = DamageType.DirectDamage;

        // [Title("伤害目标配置")] [SerializeField] private  bool toEnemy;
        // [SerializeField] private  bool toAlly;
        // [SerializeField] private  bool toSelf;

        [Inject] private DamageManager _damageManager;
        [Inject] private AlgorithmManager _algorithmManager;

        public override void OnBuffTick(Runtime.Buff buff)
        {
            // 伤害和治疗的区别就是伤害为正数，治疗为负数，所以在内部手动将正负数转换
            var fixedValue = damageType.IsHeal() ? -fixedDamage : fixedDamage;
            var multiplier = damageType.IsHeal() ? -damageMultiplier : damageMultiplier;

            // // 判断是否对敌人生效，不生效则过滤敌人
            // if (buff.carrier.Parameters.side != buff.caster.Parameters.side && !toEnemy)
            // {
            //     return;
            // }
            //
            // // 判断是否对友军生效，不生效则过滤友军
            // if (buff.carrier != buff.caster && buff.carrier.Parameters.side == buff.caster.Parameters.side && !toAlly)
            // {
            //     return;
            // }
            //
            // // 判断是否对自身生效，不生效则过滤自身
            // if (buff.carrier == buff.caster && toSelf)
            // {
            //     return;
            // }

            // 计算伤害
            var damageValue = _algorithmManager.DamageAttackConvertSO.ConvertToDamageValue(
                originFixedDamage: fixedValue,
                originDamageTimes: multiplier,
                damageType: damageType,
                attacker: buff.caster.Parameters
            );
            // 如果层数影响伤害，计算层数影响后的伤害
            if (stackImpactToDamage)
            {
                var times = 0f;
                if (stackDamageTimes != null)
                {
                    foreach (var damageTimes in stackDamageTimes)
                    {
                        if (buff.stack >= damageTimes.minStack && buff.stack <= damageTimes.maxStack)
                        {
                            times = damageTimes.damageTimes;
                            break;
                        }
                    }
                }

                damageValue *= times;
            }

            // 计算暴击率
            var criticalRate =
                allowCalculateCriticalRate
                    ? _algorithmManager.DamageCriticalRateCalculateSO.CalculateCriticalRate(damageType,
                        buff.caster.Parameters.property)
                    : 0f;
            // 计算伤害方向
            var attackerCenter = buff.caster.Visual.Center.position;
            var defenderCenter = buff.carrier.Visual.Center.position;
            var damageDirection = defenderCenter - attackerCenter;
            // 最终添加伤害
            _damageManager?.AddDamage(
                source: buff.caster,
                target: buff.carrier,
                method: new DamageBuffMethod
                {
                    Name = buff.info.name
                },
                type: damageType,
                value: damageValue,
                resourceMultiplier: resourceMultiplier,
                criticalRate: criticalRate,
                direction: damageDirection,
                ignoreSideLimit: true
            );
        }
    }
}