using System;
using System.Collections.Generic;
using Character;
using Character.Data;
using Common;
using Damage;
using Damage.Data;
using Damage.Data.Extension;
using Framework.Common.BehaviourTree.Node;
using Framework.Common.Debug;
using Framework.Common.Timeline.Data;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;
using VContainer;

namespace Skill.Unit.Feature
{
    [NodeMenuItem("Feature/Damage")]
    public class SkillFlowFeatureDamageNode : SkillFlowFeatureNode
    {
        [Title("伤害数值配置")] [InfoBox("固定伤害数值，和角色属性不挂钩，数值都不得小于0")]
        public DamageValue fixedDamage = DamageValue.Zero;

        [InfoBox("攻击力伤害系数，和角色属性挂钩，数值都不得小于0")]
        public DamageValueMultiplier damageMultiplier = DamageValueMultiplier.Zero;

        [InfoBox("影响是否暴击的因素之一")] public bool allowCalculateCriticalRate = false;

        [InfoBox("最终伤害转资源系数，数值都不得小于0")]
        public DamageResourceMultiplier resourceMultiplier = DamageResourceMultiplier.Hp;

        [Title("伤害类型配置")] [InfoBox("伤害类型，注意普通伤害、真实伤害和普通治疗的区分")]
        public DamageType damageType = DamageType.DirectDamage;

        [Title("伤害目标配置")] public bool toEnemy;
        public bool toAlly;
        public bool toSelf;

#if UNITY_EDITOR
        [Title("测试配置")] [InlineButton("TestDamageAndResource", label: "测试伤害和资源")]
        public AlgorithmDamageAndResourceAndAtbTestParameters testParameters;

        private void TestDamageAndResource()
        {
            // 伤害和治疗的区别就是伤害为正数，治疗为负数，所以在内部手动将正负数转换
            var fixedValue = damageType.IsHeal() ? -fixedDamage : fixedDamage;
            var multiplier = damageType.IsHeal() ? -damageMultiplier : damageMultiplier;
            new ConfiguredAlgorithmTest().TestDamageAndResourceAndAtb(
                testParameters.attackerReaction,
                testParameters.attackerLuck,
                testParameters.attackerPhysicsAttack,
                testParameters.attackerMagicAttack,
                new DamageSkillMethod
                {
                    Name = skillFlow.Name
                },
                fixedValue,
                multiplier,
                damageType,
                resourceMultiplier,
                testParameters.defenderReaction,
                testParameters.defenderDefence,
                1f,
                DamageValueType.None,
                DamageValueType.None
            );
        }
#endif

        [Inject] private DamageManager _damageManager;

        private DamageManager DamageManager
        {
            get
            {
                if (!_damageManager)
                {
                    _damageManager = GameEnvironment.FindEnvironmentComponent<DamageManager>();
                }

                return _damageManager;
            }
        }

        [Inject] private AlgorithmManager _algorithmManager;

        private AlgorithmManager AlgorithmManager
        {
            get
            {
                if (!_algorithmManager)
                {
                    _algorithmManager = GameEnvironment.FindEnvironmentComponent<AlgorithmManager>();
                }

                return _algorithmManager;
            }
        }

        public override string Title => "业务——伤害节点\n必须有角色列表的输入";

        public override ISkillFlowFeaturePayloadsRequire GetPayloadsRequire()
        {
            return SkillFlowFeaturePayloadsRequirements.CharactersPayloads;
        }

        /// <summary>
        /// 伤害节点执行
        /// </summary>
        /// <param name="timelineInfo"></param>
        /// <param name="payloads">这里规定0：目标角色列表</param>
        protected override void OnExecute(TimelineInfo timelineInfo, object[] payloads)
        {
            // 防错误传参
            List<CharacterObject> targets;
            try
            {
                targets = payloads[0] as List<CharacterObject>;
            }
            catch (Exception e)
            {
                DebugUtil.LogWarning($"{GetType().Name} payloads is wrong");
                return;
            }

            // 伤害和治疗的区别就是伤害为正数，治疗为负数，所以在内部手动将正负数转换
            var fixedValue = damageType.IsHeal() ? -fixedDamage : fixedDamage;
            var multiplier = damageType.IsHeal() ? -damageMultiplier : damageMultiplier;

            // 遍历目标角色执行伤害逻辑
            targets!.ForEach(target =>
            {
                // 判断是否对敌人生效，不生效则过滤敌人
                if (target.Parameters.side != Caster.Parameters.side && !toEnemy)
                {
                    return;
                }

                // 判断是否对友军生效，不生效则过滤友军
                if (target != Caster && target.Parameters.side == Caster.Parameters.side && !toAlly)
                {
                    return;
                }

                // 判断是否对自身生效，不生效则过滤自身
                if (target == Caster && !toSelf)
                {
                    return;
                }

                // 计算伤害
                var damageValue = AlgorithmManager.DamageAttackConvertSO.ConvertToDamageValue(
                    originFixedDamage: fixedValue,
                    originDamageTimes: multiplier,
                    damageType: damageType,
                    attacker: Caster.Parameters
                );
                // 计算暴击率
                var criticalRate =
                    allowCalculateCriticalRate
                        ? AlgorithmManager.DamageCriticalRateCalculateSO.CalculateCriticalRate(damageType,
                            Caster.Parameters.property)
                        : 0f;
                // 计算伤害方向
                var damageDirection =
                    Vector3.ProjectOnPlane(target.Visual.Center.position - Caster.Visual.Center.position, Vector3.up);
                // 最终添加伤害
                DamageManager?.AddDamage(
                    source: Caster,
                    target: target,
                    method: new DamageSkillMethod
                    {
                        Name = skillFlow.Name
                    },
                    type: damageType,
                    value: damageValue,
                    resourceMultiplier: resourceMultiplier,
                    criticalRate: criticalRate,
                    direction: damageDirection,
                    ignoreSideLimit: true
                );
            });
        }
    }
}