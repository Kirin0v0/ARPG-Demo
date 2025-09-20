using System;
using Character;
using Character.Data;
using Damage.Data;
using Damage.Data.Extension;
using Framework.Common.Debug;
using Player;
using Player.Ability;
using Player.StateMachine.Action;
using Player.StateMachine.Defence;
using Sirenix.Utilities;
using UnityEngine;
using VContainer;

namespace Common
{
    public class AtbManager : MonoBehaviour
    {
        [Inject] private AlgorithmManager _algorithmManager;
        [Inject] private GameManager _gameManager;

        [SerializeField] private bool debugGetAtb = false;

        private void FixedUpdate()
        {
            _gameManager.Characters.ForEach(character =>
            {
                // 如果角色死亡或不在战斗中，则将其Atb量清除
                if (character.Parameters.dead || character.Parameters.battleState != CharacterBattleState.Battle)
                {
                    character.ResourceAbility.Modify(new CharacterResource
                    {
                        atb = -character.Parameters.resource.atb
                    });
                    return;
                }

                // 每帧累加Atb量
                var accumulateAtb =
                    _algorithmManager.AtbAccumulateSO.AccumulateAtb(character.Parameters.property, Time.fixedDeltaTime);
                character.ResourceAbility.Modify(new CharacterResource
                {
                    atb = accumulateAtb
                });
            });
        }

        public void RewardAtb(DamageInfo damageInfo)
        {
            // 计算伤害本身的Atb奖励量
            var damageAtbReward =
                _algorithmManager.AtbConvertSO.ConvertToAtb(damageInfo, damageInfo.Source.Parameters.property,
                    damageInfo.Target.Parameters.property);
            damageInfo.Source.ResourceAbility.Modify(new CharacterResource
            {
                atb = damageAtbReward.attackerAtb,
            });
            damageInfo.Target.ResourceAbility.Modify(new CharacterResource
            {
                atb = damageAtbReward.defenderAtb,
            });
            if (debugGetAtb)
            {
                DebugUtil.LogCyan($"角色({damageInfo.Source.Parameters.DebugName})Atb因攻击奖励{damageAtbReward.attackerAtb}");
                DebugUtil.LogCyan($"角色({damageInfo.Target.Parameters.DebugName})Atb因受伤补偿{damageAtbReward.defenderAtb}");
            }

            // 伤害目标角色额外奖励Atb场景
            if (damageInfo.Target.AtbRewardAbility)
            {
                // 检测本次伤害是否触发完美闪避的场景，是则调用目标角色本身奖励能力，交由其处理
                if ((damageInfo.TriggerFlags & DamageInfo.PerfectEvadeFlag) != 0 &&
                    damageInfo.Target.AtbRewardAbility is PlayerAtbRewardAbility playerAtbRewardAbility1)
                {
                    var reward = playerAtbRewardAbility1.RewardAvoidDamageByPerfectEvade(damageInfo.Source);
                    damageInfo.Target.ResourceAbility.Modify(new CharacterResource
                    {
                        atb = reward,
                    });
                    if (debugGetAtb)
                    {
                        DebugUtil.LogCyan($"角色({damageInfo.Target.Parameters.DebugName})Atb因完美闪避奖励{reward}");
                    }
                }
                
                // 检测本次伤害是否触发完美防御的场景，是则调用目标角色本身奖励能力，交由其处理
                if ((damageInfo.TriggerFlags & DamageInfo.PerfectDefenceFlag) != 0 &&
                    damageInfo.Target.AtbRewardAbility is PlayerAtbRewardAbility playerAtbRewardAbility2)
                {
                    var reward = playerAtbRewardAbility2.RewardAvoidDamageByPerfectEvade(damageInfo.Source);
                    damageInfo.Target.ResourceAbility.Modify(new CharacterResource
                    {
                        atb = reward,
                    });
                    if (debugGetAtb)
                    {
                        DebugUtil.LogCyan($"角色({damageInfo.Target.Parameters.DebugName})Atb因完美防御奖励{reward}");
                    }
                }
            }

            // 伤害来源角色额外奖励Atb场景
            if (damageInfo.Source.AtbRewardAbility)
            {
                // 检测本次伤害是否导致目标硬直的场景，是则调用攻击角色本身奖励能力，交由其处理
                if (damageInfo.Target.StateAbility.CausedIntoStunnedDamageInfo.HasValue &&
                    damageInfo.Target.StateAbility.CausedIntoStunnedDamageInfo.Value.SerialNumber ==
                    damageInfo.SerialNumber)
                {
                    var reward = damageInfo.Source.AtbRewardAbility.RewardMakeTargetStunned(damageInfo.Target);
                    damageInfo.Source.ResourceAbility.Modify(new CharacterResource
                    {
                        atb = reward,
                    });
                    if (debugGetAtb)
                    {
                        DebugUtil.LogCyan($"角色({damageInfo.Source.Parameters.DebugName})Atb因目标硬直奖励{reward}");
                    }
                }

                // 检测本次伤害是否导致目标破防的场景，是则调用攻击角色本身奖励能力，交由其处理
                if (damageInfo.Target.StateAbility.CausedIntoBrokenDamageInfo.HasValue &&
                    damageInfo.Target.StateAbility.CausedIntoBrokenDamageInfo.Value.SerialNumber ==
                    damageInfo.SerialNumber)
                {
                    var reward = damageInfo.Source.AtbRewardAbility.RewardMakeTargetBroken(damageInfo.Target);
                    damageInfo.Source.ResourceAbility.Modify(new CharacterResource
                    {
                        atb = reward,
                    });
                    if (debugGetAtb)
                    {
                        DebugUtil.LogCyan($"角色({damageInfo.Source.Parameters.DebugName})Atb因目标破防奖励{reward}");
                    }
                }

                // 检测本次伤害是否导致目标死亡的场景，是则调用攻击角色本身奖励能力，交由其处理
                if (damageInfo.Target.StateAbility.CausedDeadDamageInfo.HasValue &&
                    damageInfo.Target.StateAbility.CausedDeadDamageInfo.Value.SerialNumber ==
                    damageInfo.SerialNumber)
                {
                    var reward = damageInfo.Source.AtbRewardAbility.RewardMakeTargetDead(damageInfo.Target);
                    damageInfo.Source.ResourceAbility.Modify(new CharacterResource
                    {
                        atb = reward,
                    });
                    if (debugGetAtb)
                    {
                        DebugUtil.LogCyan($"角色({damageInfo.Source.Parameters.DebugName})Atb因目标死亡奖励{reward}");
                    }
                }
            }
        }
    }
}