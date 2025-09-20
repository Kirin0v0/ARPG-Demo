using System.Collections.Generic;
using Humanoid.Data;
using Humanoid.Weapon.Data;

namespace Package.Data
{
    public class PackageGearData: PackageData
    {
        public HumanoidAppearanceGearPart Part;
        public HumanoidAppearanceGearInfoData Appearance;
        public HumanoidAppearanceRace Races;
        public int MaxHp;
        public int MaxMp;
        public int Stamina;
        public int Strength;
        public int Magic;
        public int Reaction;
        public int Luck;
    }
}