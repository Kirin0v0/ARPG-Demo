using System;
using Buff.Data;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Buff.Config.Logic.Add
{
    [Serializable]
    public abstract class BaseBuffAddLogic
    {
        public abstract void OnBuffAdd(Runtime.Buff buff);
    }
}