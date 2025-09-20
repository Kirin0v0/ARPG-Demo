using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace Character.Data
{
    [Serializable]
    public struct CharacterProperty
    {
        public int maxHp; // 最大hp值
        public int maxMp; // 最大mp值
        public float stunMeter; // 硬直量表
        public float stunReduceSpeed; // 硬直减少速率，单位是秒
        public float breakMeter; // 破防量表
        public float breakReduceSpeed; // 破防减少速率，单位是秒
        public int atbLimit; // Atb极限量
        public int stamina; // 耐力，决定防御力
        public int strength; // 力量，决定物理攻击力
        public int magic; // 魔力，决定魔法攻击力
        public int reaction; // 反应，决定Atb获取量
        public int luck; // 幸运，决定攻击暴击率

        public static CharacterProperty Zero = new()
        {
            maxHp = 0,
            maxMp = 0,
            stunMeter = 0,
            stunReduceSpeed = 0,
            breakMeter = 0,
            breakReduceSpeed = 0,
            atbLimit = 0,
            stamina = 0,
            strength = 0,
            magic = 0,
            reaction = 0,
            luck = 0
        };

        public static CharacterProperty CharacterDefault = new()
        {
            maxHp = 10,
        };

        public static CharacterProperty operator +(CharacterProperty a, CharacterProperty b)
        {
            return new CharacterProperty
            {
                maxHp = a.maxHp + b.maxHp,
                maxMp = a.maxMp + b.maxMp,
                stunMeter = a.stunMeter + b.stunMeter,
                stunReduceSpeed = a.stunReduceSpeed + b.stunReduceSpeed,
                breakMeter = a.breakMeter + b.breakMeter,
                breakReduceSpeed = a.breakReduceSpeed + b.breakReduceSpeed,
                atbLimit = a.atbLimit + b.atbLimit,
                stamina = a.stamina + b.stamina,
                strength = a.strength + b.strength,
                magic = a.magic + b.magic,
                reaction = a.reaction + b.reaction,
                luck = a.luck + b.luck
            };
        }

        public static CharacterProperty operator -(CharacterProperty a, CharacterProperty b)
        {
            return new CharacterProperty
            {
                maxHp = a.maxHp - b.maxHp,
                maxMp = a.maxMp - b.maxMp,
                stunMeter = a.stunMeter - b.stunMeter,
                stunReduceSpeed = a.stunReduceSpeed - b.stunReduceSpeed,
                breakMeter = a.breakMeter - b.breakMeter,
                breakReduceSpeed = a.breakReduceSpeed - b.breakReduceSpeed,
                atbLimit = a.atbLimit - b.atbLimit,
                stamina = a.stamina - b.stamina,
                strength = a.strength - b.strength,
                magic = a.magic - b.magic,
                reaction = a.reaction - b.reaction,
                luck = a.luck - b.luck
            };
        }

        public static CharacterProperty operator *(CharacterProperty a, float b)
        {
            return new CharacterProperty
            {
                maxHp = (int)(a.maxHp * b),
                maxMp = (int)(a.maxMp * b),
                stunMeter = a.stunMeter * b,
                stunReduceSpeed = a.stunReduceSpeed * b,
                breakMeter = a.breakMeter * b,
                breakReduceSpeed = a.breakReduceSpeed * b,
                atbLimit = (int)(a.atbLimit * b),
                stamina = (int)(a.stamina * b),
                strength = (int)(a.strength * b),
                magic = (int)(a.magic * b),
                reaction = (int)(a.reaction * b),
                luck = (int)(a.luck * b)
            };
        }

        public static CharacterProperty operator *(CharacterProperty a, CharacterPropertyMultiplier b)
        {
            return new CharacterProperty
            {
                maxHp = (int)(a.maxHp * b.maxHp),
                maxMp = (int)(a.maxMp * b.maxMp),
                stunMeter = a.stunMeter * b.stunMeter,
                stunReduceSpeed = a.stunReduceSpeed * b.stunReduceSpeed,
                breakMeter = a.breakMeter * b.breakMeter,
                breakReduceSpeed = a.breakReduceSpeed * b.breakReduceSpeed,
                atbLimit = (int)(a.atbLimit * b.atbLimit),
                stamina = (int)(a.stamina * b.stamina),
                strength = (int)(a.strength * b.strength),
                magic = (int)(a.magic * b.magic),
                reaction = (int)(a.reaction * b.reaction),
                luck = (int)(a.luck * b.luck)
            };
        }

        public CharacterProperty Check()
        {
            return new CharacterProperty
            {
                maxHp = Mathf.Max(0, maxHp),
                maxMp = Mathf.Max(0, maxMp),
                stunMeter = Mathf.Max(0, stunMeter),
                stunReduceSpeed = Mathf.Max(0, stunReduceSpeed),
                breakMeter = Mathf.Max(0, breakMeter),
                breakReduceSpeed = Mathf.Max(0, breakReduceSpeed),
                atbLimit = Mathf.Max(0, atbLimit),
                stamina = Mathf.Max(0, stamina),
                strength = Mathf.Max(0, strength),
                magic = Mathf.Max(0, magic),
                reaction = Mathf.Max(0, reaction),
                luck = Mathf.Max(0, luck),
            };
        }
    }

    [Serializable]
    public struct CharacterPropertyMultiplier
    {
        public float maxHp; // 最大hp值乘积
        public float maxMp; // 最大mp值乘积
        public float stunMeter; // 硬直量表乘积
        public float stunReduceSpeed; // 硬直减少速率乘积
        public float breakMeter; // 破防量表乘积
        public float breakReduceSpeed; // 破防减少速率乘积
        public float atbLimit; // Atb极限量乘积
        public float stamina; // 耐力乘积
        public float strength; // 力量乘积
        public float magic; // 魔法乘积
        public float reaction; // 反应乘积
        public float luck; // 运气乘积

        public static CharacterPropertyMultiplier Zero = new()
        {
            maxHp = 0,
            maxMp = 0,
            stunMeter = 0,
            stunReduceSpeed = 0,
            breakMeter = 0,
            breakReduceSpeed = 0,
            atbLimit = 0,
            stamina = 0,
            strength = 0,
            magic = 0,
            reaction = 0,
            luck = 0
        };

        public static CharacterPropertyMultiplier DefaultTimes = new()
        {
            maxHp = 1,
            maxMp = 1,
            stunMeter = 1,
            stunReduceSpeed = 1,
            breakMeter = 1,
            breakReduceSpeed = 1,
            atbLimit = 1,
            stamina = 1,
            strength = 1,
            magic = 1,
            reaction = 1,
            luck = 1
        };

        public static CharacterPropertyMultiplier operator +(CharacterPropertyMultiplier a,
            CharacterPropertyMultiplier b)
        {
            return new CharacterPropertyMultiplier
            {
                maxHp = a.maxHp + b.maxHp,
                maxMp = a.maxMp + b.maxMp,
                stunMeter = a.stunMeter + b.stunMeter,
                stunReduceSpeed = a.stunReduceSpeed + b.stunReduceSpeed,
                breakMeter = a.breakMeter + b.breakMeter,
                breakReduceSpeed = a.breakReduceSpeed + b.breakReduceSpeed,
                atbLimit = a.atbLimit + b.atbLimit,
                stamina = a.stamina + b.stamina,
                strength = a.strength + b.strength,
                magic = a.magic + b.magic,
                reaction = a.reaction + b.reaction,
                luck = a.luck + b.luck
            };
        }

        public static CharacterPropertyMultiplier operator -(CharacterPropertyMultiplier a,
            CharacterPropertyMultiplier b)
        {
            return new CharacterPropertyMultiplier
            {
                maxHp = a.maxHp - b.maxHp,
                maxMp = a.maxMp - b.maxMp,
                stunMeter = a.stunMeter - b.stunMeter,
                stunReduceSpeed = a.stunReduceSpeed - b.stunReduceSpeed,
                breakMeter = a.breakMeter - b.breakMeter,
                breakReduceSpeed = a.breakReduceSpeed - b.breakReduceSpeed,
                atbLimit = a.atbLimit - b.atbLimit,
                stamina = a.stamina - b.stamina,
                strength = a.strength - b.strength,
                magic = a.magic - b.magic,
                reaction = a.reaction - b.reaction,
                luck = a.luck - b.luck
            };
        }

        public static CharacterPropertyMultiplier operator *(CharacterPropertyMultiplier a,
            CharacterPropertyMultiplier b)
        {
            return new CharacterPropertyMultiplier
            {
                maxHp = a.maxHp * b.maxHp,
                maxMp = a.maxMp * b.maxMp,
                stunMeter = a.stunMeter * b.stunMeter,
                stunReduceSpeed = a.stunReduceSpeed * b.stunReduceSpeed,
                breakMeter = a.breakMeter * b.breakMeter,
                breakReduceSpeed = a.breakReduceSpeed * b.breakReduceSpeed,
                atbLimit = a.atbLimit * b.atbLimit,
                stamina = a.stamina * b.stamina,
                strength = a.strength * b.strength,
                magic = a.magic * b.magic,
                reaction = a.reaction * b.reaction,
                luck = a.luck * b.luck
            };
        }

        public static CharacterPropertyMultiplier operator *(CharacterPropertyMultiplier a, float b)
        {
            return new CharacterPropertyMultiplier
            {
                maxHp = a.maxHp * b,
                maxMp = a.maxMp * b,
                stunMeter = a.stunMeter * b,
                stunReduceSpeed = a.stunReduceSpeed * b,
                breakMeter = a.breakMeter * b,
                breakReduceSpeed = a.breakReduceSpeed * b,
                atbLimit = a.atbLimit * b,
                stamina = a.stamina * b,
                strength = a.strength * b,
                magic = a.magic * b,
                reaction = a.reaction * b,
                luck = a.luck * b
            };
        }

        public CharacterPropertyMultiplier Check()
        {
            return new CharacterPropertyMultiplier
            {
                maxHp = Mathf.Max(0, maxHp),
                maxMp = Mathf.Max(0, maxMp),
                stunMeter = Mathf.Max(0, stunMeter),
                stunReduceSpeed = Mathf.Max(0, stunReduceSpeed),
                breakMeter = Mathf.Max(0, breakMeter),
                breakReduceSpeed = Mathf.Max(0, breakReduceSpeed),
                atbLimit = Mathf.Max(0, atbLimit),
                stamina = Mathf.Max(0, stamina),
                strength = Mathf.Max(0, strength),
                magic = Mathf.Max(0, magic),
                reaction = Mathf.Max(0, reaction),
                luck = Mathf.Max(0, luck)
            };
        }
    }
}