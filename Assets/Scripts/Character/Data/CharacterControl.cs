using System;
using UnityEngine.Serialization;

namespace Character.Data
{
    [Serializable]
    public struct CharacterControl
    {
        public bool allowMove; // 是否允许移动，影响移动能力
        public bool allowRotate; // 是否允许旋转，影响旋转能力
        public bool allowInputReaction; // 是否允许响应输入，影响玩家对角色的输入
        public bool allowReleaseAbilitySkill; // 是否允许释放能力技能
        public bool allowReleaseMagicSkill; // 是否允许释放魔法技能

        public static CharacterControl Origin = new()
        {
            allowMove = true,
            allowRotate = true,
            allowInputReaction = true,
            allowReleaseAbilitySkill = true,
            allowReleaseMagicSkill = true,
        };

        public static CharacterControl Stun = new()
        {
            allowMove = true,
            allowRotate = true,
            allowInputReaction = false,
            allowReleaseAbilitySkill = false,
            allowReleaseMagicSkill = false,
        };

        public static CharacterControl Break = new()
        {
            allowMove = false,
            allowRotate = false,
            allowInputReaction = false,
            allowReleaseAbilitySkill = false,
            allowReleaseMagicSkill = false,
        };

        public static CharacterControl Dialogue = new()
        {
            allowMove = true,
            allowRotate = true,
            allowInputReaction = false,
            allowReleaseAbilitySkill = false,
            allowReleaseMagicSkill = false,
        };

        public static CharacterControl Dead = new()
        {
            allowMove = false,
            allowRotate = false,
            allowInputReaction = false,
            allowReleaseAbilitySkill = false,
            allowReleaseMagicSkill = false,
        };
        
        public static CharacterControl operator +(CharacterControl a, CharacterControl b)
        {
            return new CharacterControl
            {
                allowMove = a.allowMove && b.allowMove,
                allowRotate = a.allowRotate && b.allowRotate,
                allowInputReaction = a.allowInputReaction && b.allowInputReaction,
                allowReleaseAbilitySkill = a.allowReleaseAbilitySkill && b.allowReleaseAbilitySkill,
                allowReleaseMagicSkill = a.allowReleaseMagicSkill && b.allowReleaseMagicSkill,
            };
        }
    }
}