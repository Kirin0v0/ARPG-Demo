using System;
using Framework.Common.Debug;

namespace Buff.Config.Logic.Add
{
    [Serializable]
    public class BuffAddLogLogic : BaseBuffAddLogic
    {
        public override void OnBuffAdd(Runtime.Buff buff)
        {
            DebugUtil.LogOrange(
                $"角色{buff.caster.Parameters.DebugName}==>角色{buff.carrier.Parameters.DebugName}：添加Buff(id={buff.info.id}, name={buff.info.name}, stack={buff.stack}, duration={buff.duration})");
        }
    }
}