using System.Collections.Generic;

namespace Package.Data
{
    public class PackageItemData: PackageData
    {
        public string AppearancePrefab;
        public int Hp;
        public int Mp;
        public string BuffId;
        public int BuffStack;
        public float BuffDuration;
        public List<string> Skills;
    }
}