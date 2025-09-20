using System;
using UnityEngine;

namespace Buff.Config.Logic.Modify
{
    [Serializable]
    public abstract class BaseBuffModifyLogic
    {
        public abstract void OnBuffModify(Runtime.Buff buff, int modifyStack);
    }
}