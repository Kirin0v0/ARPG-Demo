using System;
using Framework.Common.Util;
using Package.Data;
using Package.Data.Extension;

namespace Package.Runtime
{
    public class PackageGroup
    {
        public string GroupId; // 组Id，组创建时生成，唯一Id
        public int Number; // 物品数量
        public bool New; // 物品组是否为新
        public long GetTimestamp; // 物品组创建时间
        public PackageData Data; // 物品静态数据

        public PackageType Type => Data.GetPackageType();

        public static PackageGroup CreateNew(PackageData data, int number)
        {
            return new PackageGroup
            {
                GroupId = MathUtil.RandomId(),
                Number = number,
                New = true,
                GetTimestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds(),
                Data = data,
            };
        }
    }
}