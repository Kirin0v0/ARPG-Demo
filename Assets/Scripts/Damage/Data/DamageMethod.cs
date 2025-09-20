using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace Damage.Data
{
    [Serializable]
    public abstract class DamageMethod
    {
    }

    /// <summary>
    /// 连招伤害方式的武器类型，用于描述造成伤害的武器类型
    /// </summary>
    public enum DamageComboWeaponType
    {
        Self,
        Sword,
        Shield,
        Katana,
    }

    public class DamageParryMethod : DamageMethod
    {
    }

    public class DamageComboMethod : DamageMethod
    {
        public string Name;
        public DamageComboWeaponType WeaponType;
        public string CollideDetectionChannelId;
    }

    public class DamageSkillMethod : DamageMethod
    {
        public string Name;
    }

    public class DamageBuffMethod : DamageMethod
    {
        public string Name;
    }

    /// <summary>
    /// 环境伤害方式的类型
    /// </summary>
    public enum DamageEnvironmentType
    {
        Default,
        Fall,
        DeadZone,
    }

    public class DamageEnvironmentMethod : DamageMethod
    {
        public DamageEnvironmentType Type;

        public static readonly DamageEnvironmentMethod Default = new DamageEnvironmentMethod
        {
            Type = DamageEnvironmentType.Default
        };

        public static readonly DamageEnvironmentMethod Fall = new DamageEnvironmentMethod
        {
            Type = DamageEnvironmentType.Fall
        };

        public static readonly DamageEnvironmentMethod DeadZone = new DamageEnvironmentMethod
        {
            Type = DamageEnvironmentType.DeadZone
        };
    }
}