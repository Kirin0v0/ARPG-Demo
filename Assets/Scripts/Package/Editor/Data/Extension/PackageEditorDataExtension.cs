using System.Linq;
using Character.Data.Extension;
using Framework.Common.Resource;
using Humanoid.Data;
using Humanoid.Weapon.Data;
using Humanoid.Weapon.SO;
using Package.Data;
using Package.Data.Extension;
using UnityEngine;

namespace Package.Editor.Data.Extension
{
    public static class PackageEditorDataExtension
    {
        public static PackageEditorData ToPackageEditorData(
            this PackageInfoData packageInfoData,
            HumanoidAppearanceWeaponInfoContainer weaponInfoContainer,
            HumanoidAppearanceGearInfoContainer gearInfoContainer
        )
        {
            var packageEditorData = ScriptableObject.CreateInstance<PackageEditorData>();
            var packageData = packageInfoData.ToPackageData(
                HumanoidWeaponSingletonConfigSO.Instance.GetWeaponEquipmentConfiguration,
                _ => new HumanoidWeaponAttackConfigData
                {
                    supportAttack = false,
                    attackAbility = new HumanoidWeaponAttackAbilityConfigData(),
                },
                _ => new HumanoidWeaponDefenceConfigData
                {
                    supportDefend = false,
                    defenceAbility = new HumanoidWeaponDefenceAbilityConfigData(),
                },
                weaponInfoContainer,
                gearInfoContainer
            );

            // 通用配置
            packageEditorData.WeaponInfoContainer = weaponInfoContainer;
            packageEditorData.GearInfoContainer = gearInfoContainer;
            packageEditorData.id = packageData.Id;
            packageEditorData.type = packageInfoData.GetPackageType();
            packageEditorData.name = packageData.Name;
            packageEditorData.price = packageData.Price;
            packageEditorData.introduction = packageData.Introduction;
            packageEditorData.thumbnailAtlas = packageData.ThumbnailAtlas;
            packageEditorData.thumbnailName = packageData.ThumbnailName;
            packageEditorData.quantitativeRestriction = packageData.QuantitativeRestriction;
            packageEditorData.groupMaximum = packageData.GroupMaximum;

            // 差异化配置
            switch (packageData)
            {
                case PackageWeaponData packageWeaponData:
                {
                    packageEditorData.weaponType = packageWeaponData.Type;
                    packageEditorData.weaponAppearanceId = packageWeaponData.Appearance.Id;
                    packageEditorData.defenceDamageMultiplier = packageWeaponData.DefenceDamageMultiplier;
                    packageEditorData.defenceBreakResumeSpeed = packageWeaponData.DefenceBreakResumeSpeed;
                    packageEditorData.weaponSkills = packageWeaponData.Skills;
                    packageEditorData.maxHp = packageWeaponData.MaxHp;
                    packageEditorData.maxMp = packageWeaponData.MaxMp;
                    packageEditorData.stamina = packageWeaponData.Stamina;
                    packageEditorData.strength = packageWeaponData.Strength;
                    packageEditorData.magic = packageWeaponData.Magic;
                    packageEditorData.reaction = packageWeaponData.Reaction;
                    packageEditorData.luck = packageWeaponData.Luck;
                }
                    break;
                case PackageGearData packageGearData:
                {
                    packageEditorData.gearPart = packageGearData.Part;
                    packageEditorData.gearAppearanceId = packageGearData.Appearance.Id;
                    packageEditorData.maxHp = packageGearData.MaxHp;
                    packageEditorData.maxMp = packageGearData.MaxMp;
                    packageEditorData.stamina = packageGearData.Stamina;
                    packageEditorData.strength = packageGearData.Strength;
                    packageEditorData.magic = packageGearData.Magic;
                    packageEditorData.reaction = packageGearData.Reaction;
                    packageEditorData.luck = packageGearData.Luck;
                }
                    break;
                case PackageItemData packageItemData:
                {
                    packageEditorData.useDefaultItemPrefab = string.IsNullOrEmpty(packageItemData.AppearancePrefab);
                    packageEditorData.itemAppearancePrefab = packageItemData.AppearancePrefab;
                    packageEditorData.hp = packageItemData.Hp;
                    packageEditorData.mp = packageItemData.Mp;
                    packageEditorData.buffId = packageItemData.BuffId;
                    packageEditorData.buffStack = packageItemData.BuffStack;
                    packageEditorData.buffDuration = packageItemData.BuffDuration;
                    packageEditorData.itemSkills = packageItemData.Skills;
                }
                    break;
                case PackageMaterialData packageMaterialData:
                {
                    packageEditorData.useDefaultMaterialPrefab =
                        string.IsNullOrEmpty(packageMaterialData.AppearancePrefab);
                    packageEditorData.materialAppearancePrefab = packageMaterialData.AppearancePrefab;
                }
                    break;
            }

            return packageEditorData;
        }

        public static PackageInfoData ToPackageInfoData(this PackageEditorData packageEditorData)
        {
            return new PackageInfoData
            {
                Id = packageEditorData.id,
                Type = packageEditorData.type.GetString(),
                Name = packageEditorData.name,
                Introduction = packageEditorData.introduction,
                Price = packageEditorData.price,
                Thumbnail = packageEditorData.thumbnailAtlas + "," + packageEditorData.thumbnailName,
                QuantitativeRestriction = packageEditorData.quantitativeRestriction.GetString(),
                GroupMaximum = packageEditorData.groupMaximum,
                WeaponType = packageEditorData.type == PackageType.Weapon
                    ? packageEditorData.weaponType.GetString()
                    : "",
                WeaponAppearanceId = packageEditorData.type == PackageType.Weapon
                    ? packageEditorData.weaponAppearanceId
                    : -1,
                GearPart = packageEditorData.type == PackageType.Gear ? packageEditorData.gearPart.GetString() : "",
                GearAppearanceId = packageEditorData.type == PackageType.Gear ? packageEditorData.gearAppearanceId : -1,
                MaxHp = packageEditorData.type == PackageType.Weapon || packageEditorData.type == PackageType.Gear
                    ? packageEditorData.maxHp
                    : 0,
                MaxMp = packageEditorData.type == PackageType.Weapon || packageEditorData.type == PackageType.Gear
                    ? packageEditorData.maxMp
                    : 0,
                DefenceDamageMultiplier = packageEditorData.type == PackageType.Weapon
                    ? packageEditorData.defenceDamageMultiplier
                    : 1,
                DefenceBreakResumeSpeed = packageEditorData.type == PackageType.Weapon
                    ? packageEditorData.defenceBreakResumeSpeed
                    : 1,
                Stamina = packageEditorData.type == PackageType.Weapon || packageEditorData.type == PackageType.Gear
                    ? packageEditorData.stamina
                    : 0,
                Strength = packageEditorData.type == PackageType.Weapon || packageEditorData.type == PackageType.Gear
                    ? packageEditorData.strength
                    : 0,
                Magic = packageEditorData.type == PackageType.Weapon || packageEditorData.type == PackageType.Gear
                    ? packageEditorData.magic
                    : 0,
                Reaction = packageEditorData.type == PackageType.Weapon || packageEditorData.type == PackageType.Gear
                    ? packageEditorData.reaction
                    : 0,
                Luck = packageEditorData.type == PackageType.Weapon || packageEditorData.type == PackageType.Gear
                    ? packageEditorData.luck
                    : 0,
                WeaponSkills = packageEditorData.type == PackageType.Weapon
                    ? string.Join(",", packageEditorData.weaponSkills)
                    : "",
                ItemAppearancePrefab = packageEditorData.type == PackageType.Item &&
                                       !packageEditorData.useDefaultItemPrefab
                    ? packageEditorData.itemAppearancePrefab
                    : "",
                Hp = packageEditorData.type == PackageType.Item
                    ? packageEditorData.hp
                    : 0,
                Mp = packageEditorData.type == PackageType.Item
                    ? packageEditorData.mp
                    : 0,
                BuffId = packageEditorData.type == PackageType.Item
                    ? packageEditorData.buffId
                    : "",
                BuffStack = packageEditorData.type == PackageType.Item
                    ? packageEditorData.buffStack
                    : 0,
                BuffDuration = packageEditorData.type == PackageType.Item
                    ? packageEditorData.buffDuration
                    : 0,
                ItemSkills = packageEditorData.type == PackageType.Item
                    ? string.Join(",", packageEditorData.itemSkills)
                    : "",
                MaterialAppearancePrefab = packageEditorData.type == PackageType.Material &&
                                           !packageEditorData.useDefaultMaterialPrefab
                    ? packageEditorData.materialAppearancePrefab
                    : ""
            };
        }
    }
}