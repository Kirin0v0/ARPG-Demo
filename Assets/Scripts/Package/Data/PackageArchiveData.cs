using System;
using System.Collections.Generic;

namespace Package.Data
{
    /// <summary>
    /// 背包存档数据类
    /// </summary>
    [Serializable]
    public class PackageArchiveData
    {
        public List<PackageItemArchiveData> packages = new();
        public string leftHandWeaponGroupId = "";
        public string rightHandWeaponGroupId = "";
        public string headGearGroupId = "";
        public string torsoGearGroupId = "";
        public string leftArmGearGroupId = "";
        public string rightArmGearGroupId = "";
        public string leftLegGearGroupId = "";
        public string rightLegGearGroupId = "";
    }

    /// <summary>
    /// 物品存档数据类
    /// </summary>
    [Serializable]
    public class PackageItemArchiveData
    {
        public int id;
        public string groupId;
        public int number;
        public bool isNew;
        public long getTimestamp;
    }
}