using System;
using Character.Data;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace Damage.Data
{
    /// <summary>
    /// 伤害转资源类，我们规定伤害为正，治疗为负，与伤害一致
    /// </summary>
    [Serializable]
    public struct DamageResource
    {
        public int hp;
        public int mp;
        public float stun;
        public float @break;

        public static DamageResource Zero = new DamageResource
        {
            hp = 0,
            mp = 0,
            stun = 0,
            @break = 0,
        };

        public CharacterResource Cost()
        {
            return new CharacterResource
            {
                hp = -hp, // hp花费是扣除
                mp = -mp, // mp花费是扣除
                stun = stun, // 硬直花费是添加硬直量 
                @break = @break // 破防花费是添加破防量
            };
        }
    }

    /// <summary>
    /// 伤害转资源乘区类，这里数值不能是负数
    /// </summary>
    [Serializable]
    public struct DamageResourceMultiplier
    {
        [InfoBox("伤害总和转Hp系数，非特殊情况都是1"), MinValue(0)]
        public float hp;

        [InfoBox("伤害总和转Mp系数，除了扣除蓝量外都是0"), MinValue(0)]
        public float mp;

        [InfoBox("伤害总和转硬直量系数，硬直量影响全部角色"), Range(0f, 1f)] public float stun;
        [InfoBox("伤害总和转破防量系数，破防量不影响玩家"), Range(0f, 1f)] public float @break;

        [InfoBox("伤害总和奖励Atb量系数（非唯一系数），默认为0，根据不同伤害方式设置，允许超过1"), MinValue(0)]
        public float atb;

        public static DamageResourceMultiplier Default = new DamageResourceMultiplier
        {
            hp = 1,
            mp = 0,
            stun = 0,
            @break = 0,
            atb = 0,
        };

        public static DamageResourceMultiplier Hp = new DamageResourceMultiplier
        {
            hp = 1,
            mp = 0,
            stun = 0,
            @break = 0,
            atb = 0,
        };

        public static DamageResourceMultiplier Mp = new DamageResourceMultiplier
        {
            hp = 0,
            mp = 1,
            stun = 0,
            @break = 0,
            atb = 0,
        };

        public static DamageResourceMultiplier Stun = new DamageResourceMultiplier
        {
            hp = 0,
            mp = 0,
            stun = 1,
            @break = 0,
            atb = 0,
        };

        public static DamageResourceMultiplier Break = new DamageResourceMultiplier
        {
            hp = 0,
            mp = 0,
            stun = 0,
            @break = 1,
            atb = 0,
        };

        public DamageResourceMultiplier Check()
        {
            return new DamageResourceMultiplier
            {
                hp = Mathf.Max(0, hp),
                mp = Mathf.Max(0, mp),
                stun = Mathf.Max(0, stun),
                @break = Mathf.Max(0, @break),
                atb = Mathf.Max(0, atb),
            };
        }
    }
}