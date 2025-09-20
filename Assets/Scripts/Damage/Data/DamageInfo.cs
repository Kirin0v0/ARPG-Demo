using System;
using System.Collections.Generic;
using Buff.Data;
using Character;
using Damage.Data.Extension;
using UnityEngine;

namespace Damage.Data
{
    public struct DamageInfo
    {
        public const int DefenceFlag = 1 << 0; // 防御标识，仅用于玩家作为伤害目标
        public const int BrokenFlag = 1 << 1; // 破防标识
        public const int PerfectEvadeFlag = 1 << 2; // 完美闪避标识，仅用于玩家作为伤害目标
        public const int PerfectDefenceFlag = 1 << 3; // 完美防御标识，仅用于玩家作为伤害目标
        
        public string SerialNumber; // 伤害流水号
        public CharacterObject Source; // 伤害来源
        public CharacterObject Target; // 伤害目标
        public DamageMethod Method; // 伤害方式
        public DamageType Type; // 伤害类型
        public DamageValue Value; // 伤害数值
        public DamageResourceMultiplier ResourceMultiplier; // 伤害转资源乘区
        public DamageResource Resource; // 伤害转资源
        public float CriticalRate; // 暴击率，范围为0~1
        public float HitRate; // 命中率，但在ARPG中不应该在攻击命中时丢失伤害，所以仅设计不使用
        public bool IsCritical; // 本次伤害是否暴击
        public Vector3 Direction; // 伤害打向目标的方向
        public List<BuffAddInfo> AddBuffs; // 伤害后添加Buff列表
        public List<BuffRemoveInfo> RemoveBuffs; // 伤害后删除Buff列表
        public int TriggerFlags; // 触发标识位，用于记录伤害触发的机制等
        
        /// <summary>
        /// 计算伤害转化的资源
        /// </summary>
        /// <param name="damageValue"></param>
        /// <param name="resourceMultiplier"></param>
        /// <returns></returns>
        public static DamageResource CalculateResource(DamageValue damageValue, DamageResourceMultiplier resourceMultiplier)
        {
            var multiplier = resourceMultiplier.Check();
            var hp = (int)(damageValue.Total() * multiplier.hp);
            var mp = (int)(damageValue.Total() * multiplier.mp);
            var stun = damageValue.Total() * multiplier.stun;
            var @break = damageValue.Total() * multiplier.@break;
            return new DamageResource
            {
                hp = hp,
                mp = mp,
                stun = stun,
                @break = @break,
            };
        }

        public static DamageValue CalculateDamageHpValue(DamageValue damageValue,
            DamageResourceMultiplier resourceMultiplier)
        {
            return new DamageValue
            {
                noType = (int)(damageValue.noType * resourceMultiplier.hp),
                physics = (int)(damageValue.physics * resourceMultiplier.hp),
                fire = (int)(damageValue.fire * resourceMultiplier.hp),
                ice = (int)(damageValue.ice * resourceMultiplier.hp),
                wind = (int)(damageValue.wind * resourceMultiplier.hp),
                lightning = (int)(damageValue.lightning * resourceMultiplier.hp),
            };
        }

        /// <summary>
        /// 结算伤害
        /// </summary>
        /// <param name="damageValue"></param>
        /// <param name="damageResource"></param>
        /// <param name="isCritical"></param>
        /// <returns></returns>
        public DamageInfo Settle(DamageValue damageValue, DamageResource damageResource, bool isCritical)
        {
            return new DamageInfo
            {
                SerialNumber = SerialNumber,
                Source = Source,
                Target = Target,
                Method = Method,
                Type = Type,
                Value = damageValue,
                ResourceMultiplier = ResourceMultiplier,
                Resource = damageResource,
                CriticalRate = CriticalRate,
                HitRate = HitRate,
                IsCritical = isCritical,
                Direction = Direction,
                AddBuffs = AddBuffs,
                RemoveBuffs = RemoveBuffs,
                TriggerFlags = TriggerFlags,
            };
        }
        
        public DamageInfo ResetValue(DamageValue damageValue)
        {
            return new DamageInfo
            {
                SerialNumber = SerialNumber,
                Source = Source,
                Target = Target,
                Method = Method,
                Type = Type,
                Value = damageValue,
                ResourceMultiplier = ResourceMultiplier,
                Resource = Resource,
                CriticalRate = CriticalRate,
                HitRate = HitRate,
                IsCritical = IsCritical,
                Direction = Direction,
                AddBuffs = AddBuffs,
                RemoveBuffs = RemoveBuffs,
                TriggerFlags = TriggerFlags,
            };
        }

        public DamageInfo ResetResource(DamageResource damageResource)
        {
            return new DamageInfo
            {
                SerialNumber = SerialNumber,
                Source = Source,
                Target = Target,
                Method = Method,
                Type = Type,
                Value = Value,
                ResourceMultiplier = ResourceMultiplier,
                Resource = damageResource,
                CriticalRate = CriticalRate,
                HitRate = HitRate,
                IsCritical = IsCritical,
                Direction = Direction,
                AddBuffs = AddBuffs,
                RemoveBuffs = RemoveBuffs,
                TriggerFlags = TriggerFlags,
            };
        }

        public DamageInfo SetTriggerFlags(int triggerFlags)
        {
            return new DamageInfo
            {
                SerialNumber = SerialNumber,
                Source = Source,
                Target = Target,
                Method = Method,
                Type = Type,
                Value = Value,
                ResourceMultiplier = ResourceMultiplier,
                Resource = Resource,
                CriticalRate = CriticalRate,
                HitRate = HitRate,
                IsCritical = IsCritical,
                Direction = Direction,
                AddBuffs = AddBuffs,
                RemoveBuffs = RemoveBuffs,
                TriggerFlags = triggerFlags,
            };
        }
    }
}