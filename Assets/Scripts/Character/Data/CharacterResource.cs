using System;
using UnityEngine;

namespace Character.Data
{
    [Serializable]
    public struct CharacterResource
    {
        public int hp; // hp
        public int mp; // mp
        public float stun; // 硬直量
        public float @break; // 破防量
        public float atb; // Atb量

        public static CharacterResource Empty = new()
        {
            hp = 0,
            mp = 0,
            stun = 0,
            @break = 0,
            atb = 0,
        };
        
        public CharacterResource Fill(int maxHp, int maxMp, bool remainBattleResource) => new()
        {
            hp = maxHp,
            mp = maxMp,
            stun = remainBattleResource ? stun: 0,
            @break = remainBattleResource ? @break: 0,
            atb = remainBattleResource ? atb: 0,
        };

        public CharacterResource SetStunZero()
        {
            return new CharacterResource
            {
                hp = hp,
                mp = mp,
                stun = 0,
                @break = @break,
                atb = atb,
            };
        }
        
        public CharacterResource SetBreakZero()
        {
            return new CharacterResource
            {
                hp = hp,
                mp = mp,
                stun = stun,
                @break = 0,
                atb = atb,
            };
        }
        
        public static CharacterResource operator +(CharacterResource a, CharacterResource b)
        {
            return new CharacterResource
            {
                hp = a.hp + b.hp,
                mp = a.mp + b.mp,
                stun = a.stun + b.stun,
                @break = a.@break + b.@break,
                atb = a.atb + b.atb,
            };
        }

        public static CharacterResource operator -(CharacterResource a, CharacterResource b)
        {
            return new CharacterResource
            {
                hp = a.hp - b.hp,
                mp = a.mp - b.mp,
                stun = a.stun - b.stun,
                @break = a.@break - b.@break,
                atb = a.atb - b.atb,
            };
        }

        public static CharacterResource operator -(CharacterResource a)
        {
            return new CharacterResource
            {
                hp = -a.hp,
                mp = -a.mp,
                stun = -a.stun,
                @break = -a.@break,
                atb = -a.atb,
            };
        }

        public static CharacterResource operator *(CharacterResource a, float b)
        {
            return new CharacterResource
            {
                hp = (int)(a.hp * b),
                mp = (int)(a.mp * b),
                stun = a.stun * b,
                @break = a.@break * b,
                atb = a.atb * b,
            };
        }
    }
}