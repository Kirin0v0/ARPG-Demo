using System.Collections.Generic;
using Common;
using Humanoid.Data;
using Humanoid.Weapon.Data;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Humanoid.Weapon.SO
{
    [CreateAssetMenu(menuName = "Character/Weapon/Singleton Config")]
    public class HumanoidWeaponSingletonConfigSO : SingletonSerializedScriptableObject<HumanoidWeaponSingletonConfigSO>
    {
        [LabelText("武器外观材质配置"), SerializeField]
        private Dictionary<HumanoidAppearanceWeaponType, Material> weaponAppearanceMaterials = new();

        [LabelText("武器类型装备配置"), SerializeField]
        private Dictionary<HumanoidWeaponType, HumanoidWeaponEquipmentConfigData> weaponEquipmentConfigurations = new();
        
        public Material GetWeaponAppearanceMaterial(HumanoidAppearanceWeaponType type)
        {
            if (weaponAppearanceMaterials.TryGetValue(type, out var material))
            {
                return material;
            }

            return null;
        }
        
        public HumanoidWeaponEquipmentConfigData GetWeaponEquipmentConfiguration(HumanoidWeaponType type)
        {
            if (weaponEquipmentConfigurations.TryGetValue(type, out var data))
            {
                return data;
            }

            return default;
        }
    }
}