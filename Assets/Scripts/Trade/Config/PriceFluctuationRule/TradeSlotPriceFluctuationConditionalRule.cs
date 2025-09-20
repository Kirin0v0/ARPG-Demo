using System;
using Character;

namespace Trade.Config.PriceFluctuationRule
{
    [Serializable]
    public abstract class TradeSlotPriceFluctuationConditionalRule: BaseTradeSlotPriceFluctuationRule
    {
        public enum PriceFluctuationCalculateType
        {
            Set,
            Append,
        }
        
        public PriceFluctuationCalculateType calculateType;
        public float priceFluctuation = 1f;

        public override float CalculatePriceFluctuation(float originalPriceFluctuation, CharacterObject self, CharacterObject target)
        {
            if (ValidateCondition(self, target))
            {
                return calculateType switch
                {
                    PriceFluctuationCalculateType.Set => priceFluctuation,
                    PriceFluctuationCalculateType.Append => originalPriceFluctuation + priceFluctuation,
                    _ => originalPriceFluctuation,
                };
            }

            return originalPriceFluctuation;
        }

        protected abstract bool ValidateCondition(CharacterObject self, CharacterObject target);
    }
}