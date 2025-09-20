using System;

namespace Package.Data
{
    public enum PackageType
    {
        Weapon,
        Gear,
        Item,
        Material,
    }

    public enum PackageQuantitativeRestriction
    {
        OnlyOneGroup,
        NoRestriction,
    }

    public abstract class PackageData
    {
        public int Id; // 物品Id，同一种物品预设Id相同
        public string Name;
        public string Introduction;
        public int Price;
        public string ThumbnailAtlas;
        public string ThumbnailName;
        public PackageQuantitativeRestriction QuantitativeRestriction;
        public int GroupMaximum;
    }
}