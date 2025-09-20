using System.Collections.Generic;
using Character;

namespace Buff.Data
{
    public struct BuffRemoveInfo
    {
        public BuffInfo Info; // Buff配置信息
        public CharacterObject Caster; // Buff施法者
        public CharacterObject Target; // Buff目标
        public int Stack; // Buff删除的层数，这里是负数
        public BuffRemoveDurationType DurationType; // Buff持续时间修改类型
        public float Duration; // Buff持续时间
        public Dictionary<string, object> RuntimeParams; // Buff运行时参数
    }

    public enum BuffRemoveDurationType
    {
        NotChangeDuration,
        AppendDuration,
        SetDuration,
    }
}