using System;
using System.Collections.Generic;
using Buff.Data;
using Character;
using UnityEngine.Serialization;

namespace Buff.Runtime
{
    /// <summary>
    /// Buff运行时数据，隔离预设数据和实际运行的数据
    /// </summary>
    [Serializable]
    public class Buff
    {
        public string runningNumber; // Buff运行编号，全局唯一
        public BuffInfo info; // Buff配置信息
        public bool permanent; // Buff是否是永久Buff
        public float duration; // Buff剩余时长，如果是永久Buff则不会减少
        public float elapsedTime; // Buff存在时间
        public float expectTime; // Buff期望时间，仅仅用于记录本次Buff的预设执行时间，不参与实际业务逻辑，由于Buff在实际场景中存在添加层、删除层、覆盖等操作，往往存在时间远大于期望时间
        public int stack; // Buff堆叠层数
        public int tickTimes; // Buff帧执行次数
        public CharacterObject caster; // Buff施法者
        public CharacterObject carrier; // Buff携带者
        public Dictionary<string, object> RuntimeParams = new(); // Buff运行时参数
    }
}