using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Framework.Common.Debug;
using Humanoid.Data;
using Humanoid.Weapon.Data;
using Package.Data;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Humanoid.Weapon
{
    /// <summary>
    /// 鉴于角色武器攻击/防御配置需要关联大量材质、连招以及对应的资源，所以将武器部分配置抽为Mono类方便场景加载和卸载
    /// </summary>
    public class HumanoidWeaponManager : SerializedMonoBehaviour
    {
        [Serializable]
        private class WeaponAttackConfiguration
        {
            public HumanoidWeaponType type;
            [InlineProperty]public HumanoidWeaponAttackConfigData configuration;
        }

        [Serializable]
        private class WeaponDefenceConfiguration
        {
            public HumanoidWeaponType type;
            [InlineProperty] public HumanoidWeaponDefenceConfigData configuration;
        }

        [LabelText("武器类型攻击配置"), SerializeField, ValueDropdown("GetUnusedAttackWeaponTypes")]
        private List<WeaponAttackConfiguration> weaponAttackConfigurations = new();
        
        [LabelText("武器类型防御配置"), SerializeField, ValueDropdown("GetUnusedDefenceWeaponTypes")]
        private List<WeaponDefenceConfiguration> weaponDefenceConfigurations = new();

        public HumanoidWeaponAttackConfigData GetWeaponAttackConfiguration(HumanoidWeaponType type)
        {
            foreach (var data in weaponAttackConfigurations)
            {
                if (data.type == type)
                {
                    return data.configuration;
                }
            }

            return new()
            {
                supportAttack = false,
                attackAbility = new HumanoidWeaponAttackAbilityConfigData(),
            };
        }
        public HumanoidWeaponDefenceConfigData GetWeaponDefenceConfiguration(HumanoidWeaponType type)
        {
            foreach (var data in weaponDefenceConfigurations)
            {
                if (data.type == type)
                {
                    return data.configuration;
                }
            }

            return new()
            {
                supportDefend = false,
                defenceAbility = new HumanoidWeaponDefenceAbilityConfigData(),
            };
        }

        private IEnumerable GetUnusedAttackWeaponTypes()
        {
            var weaponTypes = Enum.GetValues(typeof(HumanoidWeaponType));
            var unusedWeaponType = new List<HumanoidWeaponType>();
            foreach (var value in weaponTypes)
            {
                var weaponType = (HumanoidWeaponType)value;
                if (weaponAttackConfigurations.All(configData => configData.type != weaponType))
                {
                    unusedWeaponType.Add(weaponType);
                }
            }

            return unusedWeaponType.Select(weaponType => new ValueDropdownItem
            {
                Text = weaponType.ToString(),
                Value = new WeaponAttackConfiguration
                {
                    type = weaponType,
                    configuration = new HumanoidWeaponAttackConfigData
                    {
                        supportAttack = false,
                        attackAbility = new(),
                    },
                },
            }).ToList();
        }

        private IEnumerable GetUnusedDefenceWeaponTypes()
        {
            var weaponTypes = Enum.GetValues(typeof(HumanoidWeaponType));
            var unusedWeaponType = new List<HumanoidWeaponType>();
            foreach (var value in weaponTypes)
            {
                var weaponType = (HumanoidWeaponType)value;
                if (weaponDefenceConfigurations.All(configData => configData.type != weaponType))
                {
                    unusedWeaponType.Add(weaponType);
                }
            }

            return unusedWeaponType.Select(weaponType => new ValueDropdownItem
            {
                Text = weaponType.ToString(),
                Value = new WeaponDefenceConfiguration
                {
                    type = weaponType,
                    configuration = new HumanoidWeaponDefenceConfigData
                    {
                       supportDefend = false,
                       defenceAbility = new()
                    }
                },
            }).ToList();
        }
    }
}