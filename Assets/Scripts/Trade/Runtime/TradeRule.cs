using System;
using System.Collections.Generic;
using System.Linq;
using Character;
using Trade.Config;
using Trade.Config.PriceFluctuationRule;
using Trade.Config.VisibilityRule;

namespace Trade.Runtime
{
    [Serializable]
    public class TradeRule
    {
        public string id;
        public string configurationId = "";
        public Dictionary<int, TradeSellableSlotRule> sellableRules = new();
        public Dictionary<int, TradePayableSlotRule> payableRules = new();

        public TradePriceStrategy GetSellableSlotPriceStrategy(int slotId)
        {
            if (sellableRules.Count == 0)
            {
                return TradePriceStrategy.SellerOnly;
            }

            return sellableRules.TryGetValue(slotId, out var rule) ? rule.priceStrategy : TradePriceStrategy.SellerOnly;
        }

        public bool IsSellableSlotVisible(int slotId, CharacterObject self, CharacterObject target)
        {
            if (sellableRules.Count == 0)
            {
                return true;
            }

            return sellableRules.TryGetValue(slotId, out var rule) && rule.IsVisible(self, target);
        }

        public bool IsPayableSlotVisible(int slotId, CharacterObject self, CharacterObject target)
        {
            if (payableRules.Count == 0)
            {
                return true;
            }

            return payableRules.TryGetValue(slotId, out var rule) && rule.IsVisible(self, target);
        }

        public bool GetSellableSlotPriceFluctuation(int slotId, CharacterObject self, CharacterObject target,
            out float priceFluctuation)
        {
            if (sellableRules.TryGetValue(slotId, out var rule))
            {
                priceFluctuation = rule.CalculatePriceFluctuation(self, target);
                return true;
            }

            priceFluctuation = 1f;
            return true;
        }

        public bool GetPayableSlotPriceFluctuation(int slotId, CharacterObject self, CharacterObject target,
            out float priceFluctuation)
        {
            if (payableRules.TryGetValue(slotId, out var rule))
            {
                priceFluctuation = rule.CalculatePriceFluctuation(self, target);
                return true;
            }

            priceFluctuation = 1f;
            return true;
        }
    }

    [Serializable]
    public class TradeSlotRule
    {
        public int slotId;
        public float defaultPriceFluctuation = 1f;
        public List<BaseTradeSlotVisibilityRule> visibilitySetters = new();
        public List<BaseTradeSlotPriceFluctuationRule> priceFluctuationCalculators = new();

        public bool IsVisible(CharacterObject self, CharacterObject target)
        {
            return visibilitySetters.Count == 0 || visibilitySetters.Any(x => x.SetSlotVisibility(self, target));
        }

        public float CalculatePriceFluctuation(CharacterObject self, CharacterObject target)
        {
            if (priceFluctuationCalculators.Count == 0)
            {
                return defaultPriceFluctuation;
            }

            var priceFluctuation = defaultPriceFluctuation;
            priceFluctuationCalculators.ForEach(x =>
                priceFluctuation = x.CalculatePriceFluctuation(priceFluctuation, self, target));
            return priceFluctuation;
        }
    }

    [Serializable]
    public class TradeSellableSlotRule : TradeSlotRule
    {
        public TradePriceStrategy priceStrategy = TradePriceStrategy.SellerOnly;
    }

    [Serializable]
    public class TradePayableSlotRule : TradeSlotRule
    {
    }
}