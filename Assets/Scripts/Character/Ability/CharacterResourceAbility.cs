using System;
using Character.Data;
using Common;
using Damage.Data;
using Damage.Data.Extension;
using Framework.Common.Debug;
using Sirenix.OdinInspector;
using UnityEngine;
using VContainer;

namespace Character.Ability
{
    public class CharacterResourceAbility: BaseCharacterNecessaryAbility
    {
        private float _reduceStunTime = 0f; // 恢复硬直量间隔时间
        private float _reduceStunRate = 1f; // 恢复硬直量系数，用于特殊场景下调整硬直量恢复速度

        private float _lastDamageTime;

        protected override void OnInit()
        {
            base.OnInit();
            // 资源初始化时默认设置最大值Hp和Mp，因为存在角色属性未完全计算完毕的场景
            Owner.Parameters.resource = Owner.Parameters.resource.Fill(Int32.MaxValue, Int32.MaxValue, false);
        }

        public void FillResource(bool remainBattleResource)
        {
            Owner.Parameters.resource = RestrictResource(
                Owner.Parameters.resource.Fill(
                    Owner.Parameters.property.maxHp,
                    Owner.Parameters.property.maxMp,
                    remainBattleResource
                ),
                Owner.Parameters.property
            );
        }

        /// <summary>
        /// 检查角色资源，并约束在属性范围内
        /// </summary>
        public void CheckResource(float deltaTime)
        {
            // 在检查角色资源前根据内部恢复速率再次设置硬直恢复速度
            Owner.Parameters.property.stunReduceSpeed *= _reduceStunRate;
            // 检查角色硬直量和破防量
            CheckStun(deltaTime);
            CheckBreak(deltaTime);
            // 锁定角色资源在属性范围内
            Owner.Parameters.resource =
                RestrictResource(Owner.Parameters.resource, Owner.Parameters.property);
        }

        /// <summary>
        /// 判断角色资源是否足够，hp/mp/atb比较大小，硬直/破防量表比较剩余量
        /// </summary>
        /// <param name="resource"></param>
        /// <returns></returns>
        public bool Enough(CharacterResource resource)
        {
            return Owner.Parameters.resource.hp > resource.hp &&
                   Owner.Parameters.resource.mp >= resource.mp &&
                   Owner.Parameters.resource.atb >= resource.atb &&
                   Owner.Parameters.resource.stun + resource.stun < Owner.Parameters.property.stunMeter &&
                   Owner.Parameters.resource.@break + resource.@break < Owner.Parameters.property.breakMeter;
        }

        /// <summary>
        /// 调整角色资源，保证资源始终在范围内
        /// </summary>
        /// <param name="resource"></param>
        public void Modify(CharacterResource resource, DamageInfo? damageInfo = null)
        {
            // 如果角色处于硬直中，不再增加硬直量
            if (Owner.Parameters.stunned && resource.stun > 0)
            {
                resource = resource.SetStunZero();
            }

            // 如果角色处于破防中，不再增加破防量
            if (Owner.Parameters.broken && resource.@break > 0)
            {
                resource = resource.SetBreakZero();
            }

            // 锁定角色资源在属性范围内
            Owner.Parameters.resource =
                RestrictResource(Owner.Parameters.resource + resource, Owner.Parameters.property);

            // 记录上次受到伤害的时间
            if (damageInfo.HasValue && !damageInfo.Value.Type.IsHeal())
            {
                _lastDamageTime = Time.time;
            }

            // 如果生命值为0且角色未死亡，则角色死亡
            if (Owner.Parameters.resource.hp == 0 && !Owner.Parameters.dead)
            {
                Owner.StateAbility.BeKilled(damageInfo);
            }

            // 如果生命值大于0且角色死亡，则角色复活
            if (Owner.Parameters.resource.hp > 0 && Owner.Parameters.dead)
            {
                Owner.StateAbility.BeRespawned(damageInfo);
            }

            // 如果角色不处于硬直且硬直量达到量表极限，则角色硬直
            if (Owner.Parameters.resource.stun >= Owner.Parameters.property.stunMeter &&
                !Owner.Parameters.stunned)
            {
                Owner.StateAbility.IntoStunned(damageInfo);
            }

            // 如果角色处于硬直且硬直量恢复为0，则角色解除硬直
            if (Owner.Parameters.stunned && Owner.Parameters.resource.stun == 0)
            {
                Owner.StateAbility.ExitStunned(damageInfo);
            }

            // 如果不处于破防且破防量达到量表极限，则角色破防
            if (Owner.Parameters.resource.@break >= Owner.Parameters.property.breakMeter &&
                !Owner.Parameters.broken)
            {
                Owner.StateAbility.IntoBroken(damageInfo);
            }

            // 如果角色处于破防且破防量恢复为0，则角色解除破防
            if (Owner.Parameters.broken && Owner.Parameters.resource.@break == 0)
            {
                Owner.StateAbility.ExitBroken(damageInfo);
            }
        }

        /// <summary>
        /// 设置硬直减少时间，管理硬直窗口的基础时间
        /// </summary>
        /// <param name="reduceStunTime"></param>
        public void SetReduceStunTime(float reduceStunTime)
        {
            _reduceStunTime = reduceStunTime;
        }

        /// <summary>
        /// 设置硬直减少量速率，用于定制硬直窗口时间
        /// </summary>
        /// <param name="rate"></param>
        public void SetReduceStunRate(float rate)
        {
            _reduceStunRate = rate;
        }

        private void CheckStun(float deltaTime)
        {
            // 根据游戏设计，硬直量在三种场景上会减少：1.角色在一段时间内不受任何伤害；2.角色硬直后 3.角色死亡时
            if (Owner.Parameters.dead)
            {
                Modify(new CharacterResource { stun = -Owner.Parameters.property.stunMeter });
            }
            else if (Time.time - _lastDamageTime >= _reduceStunTime || Owner.Parameters.stunned)
            {
                Modify(new CharacterResource
                    { stun = -Owner.Parameters.property.stunReduceSpeed * deltaTime });
            }
        }

        private void CheckBreak(float deltaTime)
        {
            // 根据游戏设计，破防量仅在两种场景上会减少：1.角色破防后 2.角色死亡时
            if (Owner.Parameters.dead)
            {
                Modify(new CharacterResource { stun = -Owner.Parameters.property.breakMeter });
            }
            else if (Owner.Parameters.broken)
            {
                Modify(new CharacterResource { @break = -Owner.Parameters.property.breakReduceSpeed * deltaTime });
            }
        }

        private CharacterResource RestrictResource(CharacterResource resource, CharacterProperty property)
        {
            resource.hp = Mathf.Clamp(resource.hp, 0, property.maxHp);
            resource.mp = Mathf.Clamp(resource.mp, 0, property.maxMp);
            resource.stun = Mathf.Clamp(resource.stun, 0, property.stunMeter);
            resource.@break = Mathf.Clamp(resource.@break, 0, property.breakMeter);
            resource.atb = Mathf.Clamp(resource.atb, 0, property.atbLimit);
            return resource;
        }
    }
}