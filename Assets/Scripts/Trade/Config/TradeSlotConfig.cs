using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using Trade.Config.PriceFluctuationRule;
using Trade.Config.VisibilityRule;
using UnityEngine;
using UnityEngine.Serialization;

namespace Trade.Config
{
    public enum TradePriceStrategy
    {
        SellerOnly, // 卖方定价
        BuyerOnly, // 买方定价
        Average, // 双方价格算术平均
    }

    [Serializable]
    public class TradeSellableSlotConfig
    {
        [InlineProperty] public TradeSlotKeyConfig key;
        [InlineProperty] public TradeSellableSlotValueConfig value;
    }

    [Serializable]
    public class TradePayableSlotConfig
    {
        [InlineProperty] public TradeSlotKeyConfig key;
        [InlineProperty] public TradePayableSlotValueConfig value;
    }

    [Serializable]
    public class TradeSlotKeyConfig
    {
        [ReadOnly] public int packageId;
#if UNITY_EDITOR
        [ReadOnly] public string packageName;
        [ReadOnly] public string packageIntroduction;
#endif
    }

    [Serializable]
    public class TradeSlotValueConfig
    {
        public bool numberLimit;
        [ShowIf("numberLimit")] public int number; // 仅当存在数量限制时才会使用数量
        [FormerlySerializedAs("priceFluctuation")] public float defaultPriceFluctuation = 1f; // 相比较原价的出售波动率

        [SerializeReference, TypeFilter("GetVisibilitySetFilteredTypeList")]
        public List<BaseTradeSlotVisibilityRule> visibilitySetters = new();

        [SerializeReference, TypeFilter("GetPriceFluctuationCalculateFilteredTypeList")]
        public List<BaseTradeSlotPriceFluctuationRule> priceFluctuationCalculators = new();

        private IEnumerable<Type> GetVisibilitySetFilteredTypeList()
        {
            var q = typeof(BaseTradeSlotVisibilityRule).Assembly.GetTypes()
                .Where(x => !x.IsAbstract)
                .Where(x => !x.IsGenericTypeDefinition)
                .Where(x => typeof(BaseTradeSlotVisibilityRule).IsAssignableFrom(x));
            return q;
        }

        private IEnumerable<Type> GetPriceFluctuationCalculateFilteredTypeList()
        {
            var q = typeof(BaseTradeSlotPriceFluctuationRule).Assembly.GetTypes()
                .Where(x => !x.IsAbstract)
                .Where(x => !x.IsGenericTypeDefinition)
                .Where(x => typeof(BaseTradeSlotPriceFluctuationRule).IsAssignableFrom(x));
            return q;
        }
    }

    [Serializable]
    public class TradeSellableSlotValueConfig : TradeSlotValueConfig
    {
        public TradePriceStrategy priceStrategy = TradePriceStrategy.SellerOnly; // 价格策略，默认是卖方定价
    }

    [Serializable]
    public class TradePayableSlotValueConfig : TradeSlotValueConfig
    {
    }
}