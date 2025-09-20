using System;
using UnityEngine;

namespace Buff.Data
{
    /// <summary>
    /// Buff标签，标签可多选，受限于int位数，最多只能有31个标签
    /// 如果需要将标签更名，请使用[InspectorName]特性
    /// </summary>
    [Flags]
    public enum BuffTag
    {
        None = 0,
        [InspectorName("治疗类")]
        Tag1 = 1 << 0,
        [InspectorName("流血类")]
        Tag2 = 1 << 1,
        [InspectorName("不可影响类")]
        Tag3 = 1 << 2,
        Tag4 = 1 << 3,
        Tag5 = 1 << 4,
        Tag6 = 1 << 5,
        Tag7 = 1 << 6,
        Tag8 = 1 << 7,
        Tag9 = 1 << 8,
        Tag10 = 1 << 9,
        Tag11 = 1 << 10,
        Tag12 = 1 << 11,
        Tag13 = 1 << 12,
        Tag14 = 1 << 13,
        Tag15 = 1 << 14,
        Tag16 = 1 << 15,
        Tag17 = 1 << 16,
        Tag18 = 1 << 17,
        Tag19 = 1 << 18,
        Tag20 = 1 << 19,
        Tag21 = 1 << 20,
        Tag22 = 1 << 21,
        Tag23 = 1 << 22,
        Tag24 = 1 << 23,
        Tag25 = 1 << 24,
        Tag26 = 1 << 25,
        Tag27 = 1 << 26,
        Tag28 = 1 << 27,
        Tag29 = 1 << 28,
        Tag30 = 1 << 29,
        Tag31 = 1 << 30,
    }
}