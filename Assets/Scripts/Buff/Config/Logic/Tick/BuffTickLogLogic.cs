using System;
using Framework.Common.Debug;

namespace Buff.Config.Logic.Tick
{
    [Serializable]
    public class BuffTickLogLogic : BaseBuffTickLogic
    {
        public override void OnBuffTick(Runtime.Buff buff)
        {
            DebugUtil.LogOrange(
                $"角色{buff.caster.Parameters.DebugName}==>角色{buff.carrier.Parameters.DebugName}：逻辑帧执行Buff(id={buff.info.id}, name={buff.info.name}, stack={buff.stack}, duration={buff.duration})");
        }
    }
}