using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Framework.Core.Extension;
using Package.Data;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using Trade.Config;
using Trade.Config.PriceFluctuationRule;
using Trade.Config.VisibilityRule;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
using SerializationUtility = Sirenix.Serialization.SerializationUtility;

namespace Trade.Editor
{
    public class TradeConfigGenerator : ScriptableObject
    {
        private readonly Func<PackageInfoContainer> _getPackageInfoContainer; // 用于获取物品信息的回调

        [NonSerialized] public TradeConfig TradeConfig; // 交易配置

        [FoldoutGroup("批量生成可售出配置")] [SerializeField]
        private bool wantSellAnything = false;

        [FoldoutGroup("批量生成可售出配置")]
        [SerializeField, HideIf("wantSellAnything"), ValueDropdown("GetAvailablePackageIds", IsUniqueList = true)]
        private List<int> wantSellPackages = new();

        [FoldoutGroup("批量生成可售出配置")] [SerializeField]
        private bool wantSellNumberLimit = false;

        [FoldoutGroup("批量生成可售出配置")] [ShowIf("wantSellNumberLimit"), SerializeField]
        public int wantSellNumber = 0; // 仅当存在数量限制时才会使用数量

        [FoldoutGroup("批量生成可售出配置")] [SerializeField]
        private float wantSellPriceFluctuation = 1f;

        [FoldoutGroup("批量生成可售出配置")] [SerializeField]
        private TradePriceStrategy wantSellPriceStrategy = TradePriceStrategy.SellerOnly;

        [FoldoutGroup("批量生成可售出配置")] [SerializeReference, TypeFilter("GetVisibilitySetFilteredTypeList")]
        public List<BaseTradeSlotVisibilityRule> wantSellVisibilitySetters = new();

        [FoldoutGroup("批量生成可售出配置")] [SerializeReference, TypeFilter("GetPriceFluctuationCalculateFilteredTypeList")]
        public List<BaseTradeSlotPriceFluctuationRule> wantSellPriceFluctuationCalculators = new();

        [FoldoutGroup("批量生成可售出配置")]
        [Button("批量生成并插入配置")]
        private void BatchGenerateSellableSlotConfigs()
        {
            if (!TradeConfig)
            {
                return;
            }

            var packageInfoContainer = _getPackageInfoContainer?.Invoke();
            if (packageInfoContainer == null)
            {
                return;
            }

            if (wantSellAnything)
            {
                packageInfoContainer.Data.Values.ForEach((packageInfoData, index) =>
                {
                    TradeConfig.sellableSlots.Add(new TradeSellableSlotConfig
                    {
                        key = new TradeSlotKeyConfig
                        {
                            packageId = packageInfoData.Id,
                            packageName = packageInfoData.Name,
                            packageIntroduction = packageInfoData.Introduction,
                        },
                        value = new TradeSellableSlotValueConfig
                        {
                            numberLimit = wantSellNumberLimit,
                            number = wantSellNumber,
                            defaultPriceFluctuation = wantSellPriceFluctuation,
                            visibilitySetters = wantSellVisibilitySetters.Select(setter =>
                                setter.Clone() as BaseTradeSlotVisibilityRule).ToList(),
                            priceFluctuationCalculators = wantSellPriceFluctuationCalculators.Select(calculator =>
                                calculator.Clone() as BaseTradeSlotPriceFluctuationRule).ToList(),
                            priceStrategy = wantSellPriceStrategy,
                        },
                    });
                });
            }
            else
            {
                wantSellPackages.ForEach(packageId =>
                {
                    if (packageInfoContainer.Data.TryGetValue(packageId, out var packageInfoData))
                    {
                        TradeConfig.sellableSlots.Add(new TradeSellableSlotConfig
                        {
                            key = new TradeSlotKeyConfig
                            {
                                packageId = packageInfoData.Id,
                                packageName = packageInfoData.Name,
                                packageIntroduction = packageInfoData.Introduction,
                            },
                            value = new TradeSellableSlotValueConfig
                            {
                                numberLimit = wantSellNumberLimit,
                                number = wantSellNumber,
                                defaultPriceFluctuation = wantSellPriceFluctuation,
                                visibilitySetters = wantSellVisibilitySetters.Select(setter =>
                                    setter.Clone() as BaseTradeSlotVisibilityRule).ToList(),
                                priceFluctuationCalculators = wantSellPriceFluctuationCalculators.Select(calculator =>
                                    calculator.Clone() as BaseTradeSlotPriceFluctuationRule).ToList(),
                                priceStrategy = wantSellPriceStrategy,
                            },
                        });
                    }
                });
            }
        }

        [FoldoutGroup("批量生成可买入配置")] [SerializeField]
        private bool wantPayAnything = false;

        [FoldoutGroup("批量生成可买入配置")]
        [SerializeField, HideIf("wantPayAnything"), ValueDropdown("GetAvailablePackageIds", IsUniqueList = true)]
        private List<int> wantPayPackages = new();

        [FoldoutGroup("批量生成可售出配置")] [SerializeField]
        private bool wantPayNumberLimit;

        [FoldoutGroup("批量生成可售出配置")] [ShowIf("wantPayNumberLimit"), SerializeField]
        public int wantPayNumber; // 仅当存在数量限制时才会使用数量

        [FoldoutGroup("批量生成可买入配置")] [SerializeField]
        private float wantPayPriceFluctuation = 1f;

        [FoldoutGroup("批量生成可买入配置")] [SerializeReference, TypeFilter("GetVisibilitySetFilteredTypeList")]
        public List<BaseTradeSlotVisibilityRule> wantPayVisibilitySetters = new();

        [FoldoutGroup("批量生成可买入配置")] [SerializeReference, TypeFilter("GetPriceFluctuationCalculateFilteredTypeList")]
        public List<BaseTradeSlotPriceFluctuationRule> wantPayPriceFluctuationCalculators = new();

        [FoldoutGroup("批量生成可买入配置")]
        [Button("批量生成并插入配置")]
        private void BatchGeneratePayableSlotConfigs()
        {
            if (!TradeConfig)
            {
                return;
            }

            var packageInfoContainer = _getPackageInfoContainer?.Invoke();
            if (packageInfoContainer == null)
            {
                return;
            }

            if (wantPayAnything)
            {
                packageInfoContainer.Data.Values.ForEach((packageInfoData, index) =>
                {
                    TradeConfig.payableSlots.Add(new TradePayableSlotConfig
                    {
                        key = new TradeSlotKeyConfig
                        {
                            packageId = packageInfoData.Id,
                            packageName = packageInfoData.Name,
                            packageIntroduction = packageInfoData.Introduction,
                        },
                        value = new TradePayableSlotValueConfig
                        {
                            numberLimit = wantPayNumberLimit,
                            number = wantPayNumber,
                            defaultPriceFluctuation = wantPayPriceFluctuation,
                            visibilitySetters = wantPayVisibilitySetters.Select(setter =>
                                setter.Clone() as BaseTradeSlotVisibilityRule).ToList(),
                            priceFluctuationCalculators = wantPayPriceFluctuationCalculators.Select(calculator =>
                                calculator.Clone() as BaseTradeSlotPriceFluctuationRule).ToList(),
                        },
                    });
                });
            }
            else
            {
                wantPayPackages.ForEach(packageId =>
                {
                    if (packageInfoContainer.Data.TryGetValue(packageId, out var packageInfoData))
                    {
                        TradeConfig.payableSlots.Add(new TradePayableSlotConfig
                        {
                            key = new TradeSlotKeyConfig
                            {
                                packageId = packageInfoData.Id,
                                packageName = packageInfoData.Name,
                                packageIntroduction = packageInfoData.Introduction,
                            },
                            value = new TradePayableSlotValueConfig
                            {
                                numberLimit = wantPayNumberLimit,
                                number = wantPayNumber,
                                defaultPriceFluctuation = wantPayPriceFluctuation,
                                visibilitySetters = wantPayVisibilitySetters.Select(setter =>
                                    setter.Clone() as BaseTradeSlotVisibilityRule).ToList(),
                                priceFluctuationCalculators = wantPayPriceFluctuationCalculators.Select(calculator =>
                                    calculator.Clone() as BaseTradeSlotPriceFluctuationRule).ToList(),
                            },
                        });
                    }
                });
            }
        }

        public TradeConfigGenerator(Func<PackageInfoContainer> getPackageInfoContainer)
        {
            _getPackageInfoContainer = getPackageInfoContainer;
        }

        private IEnumerable GetAvailablePackageIds()
        {
            var items = new List<ValueDropdownItem>();
            var packageInfoContainer = _getPackageInfoContainer?.Invoke();
            if (packageInfoContainer == null)
            {
                return items;
            }

            foreach (var pair in packageInfoContainer.Data)
            {
                items.Add(new ValueDropdownItem
                {
                    Text = $"{pair.Value.Name}({pair.Key})",
                    Value = pair.Value.Id,
                });
            }

            return items;
        }

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
}