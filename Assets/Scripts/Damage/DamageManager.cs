using System;
using System.Collections.Generic;
using Buff;
using Buff.Data;
using Character;
using Character.Data;
using Common;
using Damage.Data;
using Damage.Data.Extension;
using Framework.Common.Debug;
using Framework.Common.UI.PopupText;
using Framework.Common.Util;
using UnityEngine;
using VContainer;

namespace Damage
{
    public class DamageManager : MonoBehaviour
    {
        [Inject] private PopupTextManager _popupTextManager;
        [Inject] private BattleManager _battleManager;
        [Inject] private BuffManager _buffManager;
        [Inject] private AlgorithmManager _algorithmManager;
        [Inject] private AtbManager _atbManager;

        private readonly List<DamageInfo> _damageInfos = new();

        public event System.Action<DamageInfo> AfterDamageHandled; // 伤害处理回调

        private void FixedUpdate()
        {
            while (_damageInfos.Count > 0)
            {
                var damageInfo = _damageInfos[0];
                HandleDamage(damageInfo);
                _damageInfos.RemoveAt(0);
            }
        }

        private void OnDestroy()
        {
            _damageInfos.Clear();
        }

        /// <summary>
        /// 添加伤害到伤害管理器，交由其处理后续伤害判定
        /// 注意，在添加时，伤害仅是攻击者期望伤害，在后续会经过双方Buff计算->双方属性、弱点和免疫计算—>资源转换，才会对受击者进行资源调整
        /// </summary>
        /// <param name="source"></param>
        /// <param name="target"></param>
        /// <param name="method"></param>
        /// <param name="type"></param>
        /// <param name="value"></param>
        /// <param name="resourceMultiplier"></param>
        /// <param name="criticalRate"></param>
        /// <param name="direction"></param>
        /// <param name="ignoreSideLimit"></param>
        /// <param name="addBuffInfos"></param>
        /// <param name="removeBuffInfos"></param>
        /// <param name="cantTriggerPerfect"></param>
        public void AddDamage(
            CharacterObject source,
            CharacterObject target,
            DamageMethod method,
            DamageType type,
            DamageValue value,
            DamageResourceMultiplier resourceMultiplier,
            float criticalRate,
            Vector3 direction,
            bool ignoreSideLimit = false,
            List<BuffAddInfo> addBuffInfos = null,
            List<BuffRemoveInfo> removeBuffInfos = null
        )
        {
            value = value.Check(type.IsHeal());
            resourceMultiplier = resourceMultiplier.Check();

            var damageInfo = new DamageInfo
            {
                SerialNumber = MathUtil.RandomId(),
                Source = source,
                Target = target,
                Method = method,
                Type = type,
                Value = value,
                ResourceMultiplier = resourceMultiplier,
                CriticalRate = criticalRate,
                HitRate = 1f,
                IsCritical = false, // 默认为不暴击，等待后续算法判断是否暴击
                Direction = direction,
                AddBuffs = addBuffInfos ?? new List<BuffAddInfo>(),
                RemoveBuffs = removeBuffInfos ?? new List<BuffRemoveInfo>(),
            };

            // 这里我们在添加伤害前会判断是否忽视阵营限制
            // 阵营限制：治疗仅对同阵营起作用，伤害仅对不同阵营起作用
            if (damageInfo.Type.IsHeal())
            {
                if (source.Parameters.side != target.Parameters.side && !ignoreSideLimit)
                {
                    DebugUtil.LogOrange($"拦截治疗，来源：{source.Parameters.DebugName}，目标：{target.Parameters.DebugName}");
                    return;
                }

                DebugUtil.LogOrange($"添加治疗，来源：{source.Parameters.DebugName}，目标：{target.Parameters.DebugName}");
            }
            else
            {
                if (source.Parameters.side == target.Parameters.side && !ignoreSideLimit)
                {
                    DebugUtil.LogOrange($"拦截伤害，来源：{source.Parameters.DebugName}，目标：{target.Parameters.DebugName}");
                    return;
                }

                DebugUtil.LogOrange($"添加伤害，来源：{source.Parameters.DebugName}，目标：{target.Parameters.DebugName}");
            }

            _damageInfos.Add(damageInfo);
        }

        /// <summary>
        /// 处理伤害，这是伤害管理器的核心函数，每帧都会调用
        /// </summary>
        /// <param name="originDamageInfo"></param>
        private void HandleDamage(DamageInfo originDamageInfo)
        {
            // 如果不存在来源或目标，就直接返回
            if (!originDamageInfo.Source || !originDamageInfo.Target)
            {
                return;
            }

            // 如果是伤害而目标死亡，也直接返回
            if (!originDamageInfo.Type.IsHeal() && originDamageInfo.Target.Parameters.dead)
            {
                return;
            }

            // 这里先遍历攻击者Buff列表
            originDamageInfo.Source.Parameters.buffs.ForEach(buff =>
            {
                buff.info.OnHit?.Invoke(buff, ref originDamageInfo, originDamageInfo.Target);
            });

            // 再遍历受击者Buff列表
            originDamageInfo.Target.Parameters.buffs.ForEach(buff =>
            {
                buff.info.OnBeHurt?.Invoke(buff, ref originDamageInfo, originDamageInfo.Source);
            });

            // 在遍历Buff后再根据伤害信息计算最终伤害结算
            DamageInfo finalDamageInfo;
            if (originDamageInfo.Type.IgnoreSettlement()) // 如果是真实伤害则无视结算，即期待伤害多少就打多少，不受暴击、属性等影响
            {
                var finalResource =
                    DamageInfo.CalculateResource(originDamageInfo.Value, originDamageInfo.ResourceMultiplier);
                finalDamageInfo = originDamageInfo.Settle(originDamageInfo.Value, finalResource,
                    originDamageInfo.IsCritical);
            }
            else // 否则就要经过结算得出最终伤害
            {
                var finalDamageTuple = _algorithmManager.DamageSettlementCalculateSO.CalculateDamageSettlement(
                    originDamageInfo.Type,
                    originDamageInfo.Value,
                    originDamageInfo.CriticalRate,
                    originDamageInfo.Source.Parameters,
                    originDamageInfo.Target.Parameters
                );
                // 这里根据最终伤害结算最终资源
                var finalResource =
                    DamageInfo.CalculateResource(finalDamageTuple.damageValue,
                        originDamageInfo.ResourceMultiplier);
                finalDamageInfo = originDamageInfo.Settle(finalDamageTuple.damageValue, finalResource,
                    finalDamageTuple.isCritical);
            }

            // 如果受击者处于无敌且攻击不能穿透无敌，则将本次伤害的hp和mp值置空
            if (finalDamageInfo.Target.Parameters.immune && !originDamageInfo.Type.IgnoreImmune())
            {
                finalDamageInfo = finalDamageInfo.ResetValue(DamageValue.Zero).ResetResource(new DamageResource
                {
                    hp = 0,
                    mp = 0,
                    stun = finalDamageInfo.Resource.stun,
                    @break = finalDamageInfo.Resource.@break,
                });
            }

            // 如果受击者处于霸体，则将本次伤害的硬直值置空
            if (finalDamageInfo.Target.Parameters.endure)
            {
                finalDamageInfo = finalDamageInfo.ResetResource(new DamageResource
                {
                    hp = finalDamageInfo.Resource.hp,
                    mp = finalDamageInfo.Resource.mp,
                    stun = 0,
                    @break = finalDamageInfo.Resource.@break,
                });
            }

            // 如果受击者处于不可破防，则将本次伤害的破防值置空
            if (finalDamageInfo.Target.Parameters.unbreakable)
            {
                finalDamageInfo = finalDamageInfo.ResetResource(new DamageResource
                {
                    hp = finalDamageInfo.Resource.hp,
                    mp = finalDamageInfo.Resource.mp,
                    stun = finalDamageInfo.Resource.stun,
                    @break = 0,
                });
            }

            // 最终调整受击者伤害资源前，进行伤害的机制触发鉴定
            finalDamageInfo = finalDamageInfo.SetTriggerFlags(
                _algorithmManager.DamageTriggerMechanismSO.GetTriggerFlags(finalDamageInfo));

            // 根据最终资源调整受击者的资源，这里是伤害的本质体现
            finalDamageInfo.Target.ResourceAbility.Modify(finalDamageInfo.Resource.Cost(), finalDamageInfo);

            // 只要不是治疗且双方阵营不同，就记录战斗
            if (!finalDamageInfo.Type.IsHeal() &&
                finalDamageInfo.Source.Parameters.side != finalDamageInfo.Target.Parameters.side)
            {
                _battleManager.RecordBattle(finalDamageInfo.Source, finalDamageInfo.Target,
                    finalDamageInfo.Resource.Cost());
            }

            // 只要不是治疗，就奖励双方Atb
            if (!finalDamageInfo.Type.IsHeal())
            {
                _atbManager.RewardAtb(finalDamageInfo);
            }

            // 显示UI伤害数字
            ShowDamagePopupText(
                DamageInfo.CalculateDamageHpValue(originDamageInfo.Value, originDamageInfo.ResourceMultiplier),
                DamageInfo.CalculateResource(originDamageInfo.Value, originDamageInfo.ResourceMultiplier).Cost(),
                finalDamageInfo
            );

            // 在伤害结算后再判断目标角色是否死亡，是则遍历双方Buff执行击杀相关函数
            if (!finalDamageInfo.Type.IsHeal() && finalDamageInfo.Target.Parameters.dead)
            {
                finalDamageInfo.Source.Parameters.buffs.ForEach(buff =>
                {
                    buff.info.OnKill?.Invoke(buff, finalDamageInfo.Target);
                });
                finalDamageInfo.Target.Parameters.buffs.ForEach(buff =>
                {
                    buff.info.OnBeKilled?.Invoke(buff, finalDamageInfo.Source);
                });
            }

            // 最后执行添加Buff和移除Buff
            finalDamageInfo.AddBuffs.ForEach(addInfo => _buffManager.AddBuff(addInfo));
            finalDamageInfo.RemoveBuffs.ForEach(removeInfo => _buffManager.RemoveBuff(removeInfo));
            
            // 最终执行伤害处理的事件委托
            AfterDamageHandled?.Invoke(finalDamageInfo);
        }

        private void ShowDamagePopupText(
            DamageValue expectedDamageHpValue,
            CharacterResource expectedCostResource,
            DamageInfo finalDamageInfo
        )
        {
            var finalCostResource = finalDamageInfo.Resource.Cost();
            if (finalDamageInfo.Type.IsHeal())
            {
                if (expectedCostResource.hp != 0)
                {
                    _popupTextManager.ShowDamagePopupText(
                        $"+{finalCostResource.hp}",
                        "",
                        PopupTextType.HpHeal,
                        finalDamageInfo.Target.Visual.Center.position,
                        finalDamageInfo.IsCritical,
                        finalDamageInfo.Direction
                    );
                }

                if (expectedCostResource.mp != 0)
                {
                    _popupTextManager.ShowDamagePopupText(
                        $"+{finalCostResource.mp}Mp",
                        "",
                        PopupTextType.MpHeal,
                        finalDamageInfo.Target.Visual.Center.position,
                        finalDamageInfo.IsCritical,
                        finalDamageInfo.Direction
                    );
                }
            }
            else
            {
                var extraText = "";
                if ((finalDamageInfo.TriggerFlags & DamageInfo.PerfectDefenceFlag) != 0)
                {
                    extraText = "(完美格挡)";
                }
                else if ((finalDamageInfo.TriggerFlags & DamageInfo.PerfectEvadeFlag) != 0)
                {
                    extraText = "(完美闪避)";
                }
                else if ((finalDamageInfo.TriggerFlags & DamageInfo.DefenceFlag) != 0)
                {
                    extraText = "(格挡)";
                }
                else if ((finalDamageInfo.TriggerFlags & DamageInfo.BrokenFlag) != 0)
                {
                    extraText = "(破防增伤)";
                }

                if (expectedDamageHpValue.noType != 0)
                {
                    _popupTextManager.ShowDamagePopupText(
                        $"-{finalDamageInfo.Value.noType}",
                        extraText,
                        PopupTextType.NoTypeDamage,
                        finalDamageInfo.Target.Visual.Center.position,
                        finalDamageInfo.IsCritical,
                        finalDamageInfo.Direction
                    );
                }

                if (expectedDamageHpValue.physics != 0)
                {
                    _popupTextManager.ShowDamagePopupText(
                        $"-{finalDamageInfo.Value.physics}",
                        extraText,
                        PopupTextType.PhysicsDamage,
                        finalDamageInfo.Target.Visual.Center.position,
                        finalDamageInfo.IsCritical,
                        finalDamageInfo.Direction
                    );
                }

                if (expectedDamageHpValue.fire != 0)
                {
                    _popupTextManager.ShowDamagePopupText(
                        $"-{finalDamageInfo.Value.fire}",
                        extraText,
                        PopupTextType.FireDamage,
                        finalDamageInfo.Target.Visual.Center.position,
                        finalDamageInfo.IsCritical,
                        finalDamageInfo.Direction
                    );
                }

                if (expectedDamageHpValue.ice != 0)
                {
                    _popupTextManager.ShowDamagePopupText(
                        $"-{finalDamageInfo.Value.ice}",
                        extraText,
                        PopupTextType.IceDamage,
                        finalDamageInfo.Target.Visual.Center.position,
                        finalDamageInfo.IsCritical,
                        finalDamageInfo.Direction
                    );
                }

                if (expectedDamageHpValue.wind != 0)
                {
                    _popupTextManager.ShowDamagePopupText(
                        $"-{finalDamageInfo.Value.wind}",
                        extraText,
                        PopupTextType.WindDamage,
                        finalDamageInfo.Target.Visual.Center.position,
                        finalDamageInfo.IsCritical,
                        finalDamageInfo.Direction
                    );
                }

                if (expectedDamageHpValue.lightning != 0)
                {
                    _popupTextManager.ShowDamagePopupText(
                        $"-{finalDamageInfo.Value.lightning}",
                        extraText,
                        PopupTextType.LightningDamage,
                        finalDamageInfo.Target.Visual.Center.position,
                        finalDamageInfo.IsCritical,
                        finalDamageInfo.Direction
                    );
                }

                if (expectedCostResource.mp != 0)
                {
                    _popupTextManager.ShowDamagePopupText(
                        $"-{finalCostResource.mp}",
                        extraText,
                        PopupTextType.MpDamage,
                        finalDamageInfo.Target.Visual.Center.position,
                        finalDamageInfo.IsCritical,
                        finalDamageInfo.Direction
                    );
                }
            }
        }
    }
}