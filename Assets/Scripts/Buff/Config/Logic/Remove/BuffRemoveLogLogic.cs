using System;
using Framework.Common.Debug;

namespace Buff.Config.Logic.Remove
{
    [Serializable]
    public class BuffRemoveLogLogic: BaseBuffRemoveLogic
    {
        public override void OnBuffRemove(Runtime.Buff buff)
        {
            DebugUtil.LogOrange(
                $"角色{buff.caster.Parameters.DebugName}==>角色{buff.carrier.Parameters.DebugName}：移除Buff(id={buff.info.id}, name={buff.info.name}, stack={buff.stack}, duration={buff.duration})");
        }
    }
}