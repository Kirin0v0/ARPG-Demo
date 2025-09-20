using System;
using Character;
using Sirenix.Serialization;
using UnityEngine;

namespace Trade.Config.VisibilityRule
{
    [System.Serializable]
    public abstract class BaseTradeSlotVisibilityRule : ICloneable
    {
        public abstract bool SetSlotVisibility(CharacterObject self, CharacterObject target);

        public object Clone()
        {
            return SerializationUtility.CreateCopy(this);
        }
    }
}