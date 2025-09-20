using System;
using System.Collections.Generic;
using System.Linq;
using Character.Data.Extension;
using Framework.Common.Debug;
using Humanoid;
using Humanoid.Data;
using Humanoid.Weapon.Data;
using Package.Runtime;

namespace Package.Data.Extension
{
    public static class PackageDataExtension
    {
        /// <summary>
        /// 将Excel数据转为静态数据
        /// </summary>
        /// <param name="packageInfoData"></param>
        /// <param name="equipmentConfigurationConverter"></param>
        /// <param name="attackConfigurationConverter"></param>
        /// <param name="defenceConfigurationConverter"></param>
        /// <param name="weaponInfoContainer"></param>
        /// <param name="gearInfoContainer"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public static PackageData ToPackageData(
            this PackageInfoData packageInfoData,
            Func<HumanoidWeaponType, HumanoidWeaponEquipmentConfigData> equipmentConfigurationConverter,
            Func<HumanoidWeaponType, HumanoidWeaponAttackConfigData> attackConfigurationConverter,
            Func<HumanoidWeaponType, HumanoidWeaponDefenceConfigData> defenceConfigurationConverter,
            HumanoidAppearanceWeaponInfoContainer weaponInfoContainer,
            HumanoidAppearanceGearInfoContainer gearInfoContainer
        )
        {
            switch (packageInfoData.GetPackageType())
            {
                case PackageType.Weapon:
                {
                    var weaponData = new PackageWeaponData();
                    SetPackageUniversalFields(weaponData);
                    weaponData.Type = packageInfoData.WeaponType.ToWeaponType();
                    weaponData.Equipment = equipmentConfigurationConverter(weaponData.Type);
                    weaponData.Attack = attackConfigurationConverter(weaponData.Type);
                    weaponData.Defence = defenceConfigurationConverter(weaponData.Type);
                    weaponData.AppearanceType = weaponInfoContainer.Data[packageInfoData.WeaponAppearanceId].Type
                        .ToWeaponAppearanceType();
                    weaponData.Appearance = weaponInfoContainer.Data[packageInfoData.WeaponAppearanceId];
                    weaponData.MaxHp = packageInfoData.MaxHp;
                    weaponData.MaxMp = packageInfoData.MaxMp;
                    weaponData.DefenceDamageMultiplier = packageInfoData.DefenceDamageMultiplier;
                    weaponData.DefenceBreakResumeSpeed = packageInfoData.DefenceBreakResumeSpeed;
                    weaponData.Stamina = packageInfoData.Stamina;
                    weaponData.Strength = packageInfoData.Strength;
                    weaponData.Magic = packageInfoData.Magic;
                    weaponData.Reaction = packageInfoData.Reaction;
                    weaponData.Luck = packageInfoData.Luck;
                    weaponData.Skills = String.IsNullOrEmpty(packageInfoData.WeaponSkills)
                        ? new List<string>()
                        : packageInfoData.WeaponSkills.Split(',').ToList();
                    return weaponData;
                }
                case PackageType.Gear:
                {
                    var gearData = new PackageGearData();
                    SetPackageUniversalFields(gearData);
                    gearData.Part = packageInfoData.GetPackageGearPart();
                    gearData.Appearance = gearInfoContainer.Data[packageInfoData.GearAppearanceId];
                    gearData.Races = gearInfoContainer.Data[packageInfoData.GearAppearanceId].GetRace();
                    gearData.MaxHp = packageInfoData.MaxHp;
                    gearData.MaxMp = packageInfoData.MaxMp;
                    gearData.Stamina = packageInfoData.Stamina;
                    gearData.Strength = packageInfoData.Strength;
                    gearData.Magic = packageInfoData.Magic;
                    gearData.Reaction = packageInfoData.Reaction;
                    gearData.Luck = packageInfoData.Luck;
                    return gearData;
                }
                case PackageType.Item:
                {
                    var itemData = new PackageItemData();
                    SetPackageUniversalFields(itemData);
                    itemData.AppearancePrefab = packageInfoData.ItemAppearancePrefab;
                    itemData.Hp = packageInfoData.Hp;
                    itemData.Mp = packageInfoData.Mp;
                    itemData.BuffId = packageInfoData.BuffId;
                    itemData.BuffStack = packageInfoData.BuffStack;
                    itemData.BuffDuration = packageInfoData.BuffDuration;
                    itemData.Skills = String.IsNullOrEmpty(packageInfoData.ItemSkills)
                        ? new List<string>()
                        : packageInfoData.ItemSkills.Split(',').ToList();
                    return itemData;
                }
                case PackageType.Material:
                {
                    var materialData = new PackageMaterialData();
                    SetPackageUniversalFields(materialData);
                    materialData.AppearancePrefab = packageInfoData.MaterialAppearancePrefab;
                    return materialData;
                }
                default:
                    throw new ArgumentOutOfRangeException();
                    break;
            }

            void SetPackageUniversalFields(PackageData packageData)
            {
                var thumbnails = String.IsNullOrEmpty(packageInfoData.Thumbnail)
                    ? Array.Empty<string>()
                    : packageInfoData.Thumbnail.Split(",");
                packageData.Id = packageInfoData.Id;
                packageData.Name = packageInfoData.Name;
                packageData.Introduction = packageInfoData.Introduction;
                packageData.Price = packageInfoData.Price;
                packageData.ThumbnailAtlas = thumbnails.Length != 0 ? thumbnails[0] : "";
                packageData.ThumbnailName = thumbnails.Length != 0 ? thumbnails[1] : "";
                packageData.QuantitativeRestriction = packageInfoData.GetPackageQuantitativeRestriction();
                packageData.GroupMaximum = packageInfoData.GroupMaximum;
            }
        }

        public static PackageItemArchiveData ToItemArchiveData(this PackageGroup packageGroup)
        {
            return new PackageItemArchiveData
            {
                id = packageGroup.Data.Id,
                groupId = packageGroup.GroupId,
                number = packageGroup.Number,
                isNew = packageGroup.New,
                getTimestamp = packageGroup.GetTimestamp
            };
        }

        public static PackageGroup ToPackageGroup(
            this PackageItemArchiveData packageItemArchiveData,
            PackageInfoContainer packageInfoContainer,
            Func<HumanoidWeaponType, HumanoidWeaponEquipmentConfigData> equipmentConfigurationConverter,
            Func<HumanoidWeaponType, HumanoidWeaponAttackConfigData> attackConfigurationConverter,
            Func<HumanoidWeaponType, HumanoidWeaponDefenceConfigData> defenceConfigurationConverter,
            HumanoidAppearanceWeaponInfoContainer weaponInfoContainer,
            HumanoidAppearanceGearInfoContainer gearInfoContainer
        )
        {
            var packageData = packageInfoContainer.Data[packageItemArchiveData.id]
                .ToPackageData(equipmentConfigurationConverter, attackConfigurationConverter,
                    defenceConfigurationConverter, weaponInfoContainer, gearInfoContainer);
            return new PackageGroup
            {
                GroupId = packageItemArchiveData.groupId,
                Number = packageItemArchiveData.number,
                New = packageItemArchiveData.isNew,
                GetTimestamp = packageItemArchiveData.getTimestamp,
                Data = packageData,
            };
        }

        public static PackageType GetPackageType(this PackageInfoData packageInfoData)
        {
            return packageInfoData.Type switch
            {
                "weapon" => PackageType.Weapon,
                "gear" => PackageType.Gear,
                "item" => PackageType.Item,
                "material" => PackageType.Material,
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        public static PackageType GetPackageType(this PackageData packageData)
        {
            return packageData switch
            {
                PackageWeaponData => PackageType.Weapon,
                PackageGearData => PackageType.Gear,
                PackageItemData => PackageType.Item,
                PackageMaterialData => PackageType.Material,
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        public static string GetString(this PackageType type)
        {
            return type switch
            {
                PackageType.Weapon => "weapon",
                PackageType.Gear => "gear",
                PackageType.Item => "item",
                PackageType.Material => "material",
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        public static PackageQuantitativeRestriction GetPackageQuantitativeRestriction(
            this PackageInfoData packageInfoData)
        {
            return packageInfoData.QuantitativeRestriction switch
            {
                "only one group" => PackageQuantitativeRestriction.OnlyOneGroup,
                "no restriction" => PackageQuantitativeRestriction.NoRestriction,
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        public static bool MatchRaceRestriction(this PackageGearData packageGearData, HumanoidCharacterRace race)
        {
            return packageGearData.Races.MatchRestriction(race);
        }

        public static string GetString(this PackageQuantitativeRestriction quantitativeRestriction)
        {
            return quantitativeRestriction switch
            {
                PackageQuantitativeRestriction.OnlyOneGroup => "only one group",
                PackageQuantitativeRestriction.NoRestriction => "no restriction",
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        public static HumanoidAppearanceGearPart GetPackageGearPart(this PackageInfoData data)
        {
            return data.GearPart.ToPart();
        }

        private static HumanoidAppearanceGearPart ToPart(this string part)
        {
            return part switch
            {
                "head" => HumanoidAppearanceGearPart.Head,
                "torso" => HumanoidAppearanceGearPart.Torso,
                "left arm" => HumanoidAppearanceGearPart.LeftArm,
                "right arm" => HumanoidAppearanceGearPart.RightArm,
                "left leg" => HumanoidAppearanceGearPart.LeftLeg,
                "right leg" => HumanoidAppearanceGearPart.RightLeg,
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        public static string GetString(this HumanoidWeaponType weaponType)
        {
            return weaponType switch
            {
                HumanoidWeaponType.Sword => "sword",
                HumanoidWeaponType.Shield => "shield",
                HumanoidWeaponType.Katana => "katana",
                _ => "",
            };
        }

        private static HumanoidWeaponType ToWeaponType(this string weaponType)
        {
            return weaponType switch
            {
                "sword" => HumanoidWeaponType.Sword,
                "shield" => HumanoidWeaponType.Shield,
                "katana" => HumanoidWeaponType.Katana,
                _ => throw new ArgumentOutOfRangeException()
            };
        }
    }
}