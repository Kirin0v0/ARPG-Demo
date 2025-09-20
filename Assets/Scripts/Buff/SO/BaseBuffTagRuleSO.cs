using Buff.Data;
using UnityEngine;

namespace Buff.SO
{
    /// <summary>
    /// Buff标签规则SO类
    /// </summary>
    public abstract class BaseBuffTagRuleSO: ScriptableObject
    {
        // 在旧Buff存在时是否允许新Buff添加
        public abstract bool AllowNewBuffAddWhenOldBuffExists(BuffTag newBuffTag, BuffTag oldBuffTag);
        
        // 在新Buff添加后是否移除旧Buff
        public abstract bool IsRemovedOldBuffAfterNewBuffAdded(BuffTag oldBuffTag, BuffTag newBuffTag);
    }
}