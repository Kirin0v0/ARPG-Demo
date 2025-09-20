using System;
using UnityEngine;

namespace Buff.Config.Logic.Remove
{
    [Serializable]
    public abstract class BaseBuffRemoveLogic
    {
        public abstract void OnBuffRemove(Runtime.Buff buff);
    }
}