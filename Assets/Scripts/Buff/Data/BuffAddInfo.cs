using System.Collections.Generic;
using Character;

namespace Buff.Data
{
    public struct BuffAddInfo
    {
        public BuffInfo Info; // Buff配置信息
        public CharacterObject Caster; // Buff施法者
        public CharacterObject Target; // Buff目标
        public int Stack; // Buff添加的层数
        public bool Permanent; // Buff是否是永久Buff
        public BuffAddDurationType DurationType; // Buff持续时间修改类型
        public float Duration; // Buff持续时间
        public Dictionary<string, object> RuntimeParams; // Buff运行时参数
    }

    public enum BuffAddDurationType
    {
        NotChangeDuration,
        AppendDuration,
        SetDuration,
    }
}