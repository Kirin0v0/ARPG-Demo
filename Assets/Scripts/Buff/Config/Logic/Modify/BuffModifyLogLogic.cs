using System;
using Framework.Common.Debug;

namespace Buff.Config.Logic.Modify
{
    [Serializable]
    public class BuffModifyLogLogic : BaseBuffModifyLogic
    {
        public override void OnBuffModify(Runtime.Buff buff, int modifyStack)
        {
            DebugUtil.LogOrange(
                $"角色{buff.caster.Parameters.DebugName}==>角色{buff.carrier.Parameters.DebugName}：调整Buff层数(id={buff.info.id}, name={buff.info.name}, stack={buff.stack}, duration={buff.duration}), modifyStack={modifyStack}");
        }
    }
}