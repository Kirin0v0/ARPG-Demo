using System.Collections.Generic;
using Humanoid.Data;
using Humanoid.Weapon.Data;

namespace Package.Data
{
    public class PackageWeaponData: PackageData
    {
        public HumanoidWeaponType Type;
        public HumanoidWeaponEquipmentConfigData Equipment;
        public HumanoidWeaponAttackConfigData Attack;
        public HumanoidWeaponDefenceConfigData Defence;
        public HumanoidAppearanceWeaponType AppearanceType;
        public HumanoidAppearanceWeaponInfoData Appearance;
        public int MaxHp;
        public int MaxMp;
        public float DefenceDamageMultiplier;
        public float DefenceBreakResumeSpeed;
        public int Stamina;
        public int Strength;
        public int Magic;
        public int Reaction;
        public int Luck;
        public List<string> Skills;
    }
}