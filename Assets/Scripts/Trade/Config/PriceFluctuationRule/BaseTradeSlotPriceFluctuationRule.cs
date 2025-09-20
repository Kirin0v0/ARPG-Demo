using System;
using Character;
using Sirenix.Serialization;

namespace Trade.Config.PriceFluctuationRule
{
    [Serializable]
    public abstract class BaseTradeSlotPriceFluctuationRule : ICloneable
    {
        public abstract float CalculatePriceFluctuation(
            float originalPriceFluctuation,
            CharacterObject self,
            CharacterObject target
        );

        public object Clone()
        {
            return SerializationUtility.CreateCopy(this);
        }
    }
}