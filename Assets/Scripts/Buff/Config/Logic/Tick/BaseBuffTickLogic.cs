using System;
using UnityEngine;

namespace Buff.Config.Logic.Tick
{
    [Serializable]
    public abstract class BaseBuffTickLogic
    {
        public abstract void OnBuffTick(Runtime.Buff buff);
    }
}