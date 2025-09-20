using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Package.Data;
using Package.Data.Extension;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using Trade.Config.PriceFluctuationRule;
using Trade.Config.VisibilityRule;
using Trade.Data;
using Trade.Runtime;
using UnityEngine;
using UnityEngine.Serialization;

namespace Trade.Config
{
    [CreateAssetMenu(fileName = "Trade Config", menuName = "Trade/Trade Config")]
    public class TradeConfig : SerializedScriptableObject
    {
        [Title("交易通用配置")] public string comment = "";

        [Title("可售出配置")]
        [ListDrawerSettings(NumberOfItemsPerPage = 7)]
        [InfoBox("注意，交易槽的id本质上是列表的索引，因此更新时需在列表末尾更新，禁止插入更新")]
        public List<TradeSellableSlotConfig> sellableSlots = new();

        [Title("可买入配置")]
        [ListDrawerSettings(NumberOfItemsPerPage = 7)]
        [InfoBox("注意，交易槽的id本质上是列表的索引，因此更新时需在列表末尾更新，禁止插入更新")]
        public List<TradePayableSlotConfig> payableSlots = new();
        
        public TradeInventory GetTradeInventory(string id, PackageInfoContainer packageInfoContainer)
        {
            var sellableInventories = new Dictionary<int, TradeSlotInventory>();
            sellableSlots.ForEach((slotConfig, index) =>
            {
                sellableInventories.Add(index, new TradeSlotInventory
                {
                    slotId = index,
                    packageId = slotConfig.key.packageId,
                    numberLimit = slotConfig.value.numberLimit,
                    number = slotConfig.value.number,
                });
            });

            var payableInventories = new Dictionary<int, TradeSlotInventory>();
            payableSlots.ForEach((slotConfig, index) =>
            {
                payableInventories.Add(index, new TradeSlotInventory
                {
                    slotId = index,
                    packageId = slotConfig.key.packageId,
                    numberLimit = slotConfig.value.numberLimit,
                    number = slotConfig.value.number,
                });
            });

            return new TradeInventory
            {
                id = id,
                configurationId = GetInstanceID().ToString(),
                sellableInventories = sellableInventories,
                payableInventories = payableInventories,
            };
        }

        public TradeRule GetTradeRule(string id)
        {
            var sellableRules = new Dictionary<int, TradeSellableSlotRule>();
            sellableSlots.ForEach((slotConfig, index) =>
            {
                sellableRules.Add(index, new TradeSellableSlotRule
                {
                    slotId = index,
                    defaultPriceFluctuation = slotConfig.value.defaultPriceFluctuation,
                    visibilitySetters = slotConfig.value.visibilitySetters,
                    priceFluctuationCalculators = slotConfig.value.priceFluctuationCalculators,
                    priceStrategy = slotConfig.value.priceStrategy,
                });
            });

            var payableRules = new Dictionary<int, TradePayableSlotRule>();
            payableSlots.ForEach((slotConfig, index) =>
            {
                payableRules.Add(index, new TradePayableSlotRule
                {
                    slotId = index,
                    defaultPriceFluctuation = slotConfig.value.defaultPriceFluctuation,
                    visibilitySetters = slotConfig.value.visibilitySetters,
                    priceFluctuationCalculators = slotConfig.value.priceFluctuationCalculators,
                });
            });

            return new TradeRule
            {
                id = id,
                configurationId = GetInstanceID().ToString(),
                sellableRules = sellableRules,
                payableRules = payableRules,
            };
        }
    }
}