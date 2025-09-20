using System;

namespace Damage.Data
{
    [Flags]
    public enum DamageValueType
    {
        None = 0,
        Physics = 1 << 0,
        Fire = 1 << 1,
        Ice = 1 << 2,
        Wind = 1 << 3,
        Lightning = 1 << 4,
    }
}