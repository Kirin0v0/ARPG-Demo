using System;
using System.Collections.Generic;
using System.Linq;
using Character;
using Framework.Common.Debug;
using Framework.Common.Util;
using Map;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using Trade.Config;
using Trade.Data;
using Trade.Runtime;
using UnityEngine;
using VContainer;

namespace Trade
{
    public enum TradePresellFailureReason
    {
        Unknown,
        TradeNotExist,
        CantSell,
        UnderStock,
    }

    public enum TradePrepayFailureReason
    {
        Unknown,
        TradeNotExist,
        CantPay,
        ExceedDemand,
        MoneyNotEnough,
    }

    public class TradeManager : MonoBehaviour
    {
        public event System.Action<Runtime.Trade> OnTradeStarted;
        public event System.Action<Runtime.Trade> OnTradeManifestChanged;
        public event System.Action<Runtime.Trade> OnTradeFinished;

        [ShowInInspector, ReadOnly] private readonly List<Trade.Runtime.Trade> _ongoingTrades = new(); // 进行中的交易

        private readonly Dictionary<string, TradeInventoryArchiveData> _tradeInventoryArchives = new(); // 交易库存存档记录

        [Inject] private MapManager _mapManager;

        private void Awake()
        {
            _mapManager.BeforeMapLoad += ClearAllTrades;
        }

        private void OnDestroy()
        {
            ClearAllTrades();
            _mapManager.BeforeMapLoad -= ClearAllTrades;
        }

        public bool StartTrade(CharacterObject a, CharacterObject b, System.Action finish)
        {
            // 任意角色不具备交易能力就拒绝本次交易
            if (!a.TradeAbility || !b.TradeAbility)
            {
                DebugUtil.LogCyan($"角色({a.Parameters.DebugName})和角色({b.Parameters.DebugName})之间无法开启交易");
                return false;
            }

            // 如果该两个角色正在同时交易，则拒绝本次交易
            if (_ongoingTrades.Any(trade => trade.IsSameTrade(a, b)))
            {
                DebugUtil.LogCyan($"角色({a.Parameters.DebugName})和角色({b.Parameters.DebugName})之间无法开启交易");
                return false;
            }


            var trade = new Trade.Runtime.Trade(MathUtil.RandomId(), a, b, finish);
            trade.SetManifest(GenerateManifest(a, b));
            _ongoingTrades.Add(trade);
            DebugUtil.LogCyan($"角色({a.Parameters.DebugName})和角色({b.Parameters.DebugName})之间开启交易({trade.SerialNumber})");
            OnTradeStarted?.Invoke(trade);
            return true;
        }

        public bool Presell(CharacterObject seller, TradeOrder order, out TradePresellFailureReason failureReason)
        {
            failureReason = TradePresellFailureReason.Unknown;
            if (!seller.TradeAbility || seller.TradeAbility.Inventory == null)
            {
                return false;
            }

            if (_ongoingTrades.All(trade => trade.SerialNumber != order.TradeSerialNumber))
            {
                failureReason = TradePresellFailureReason.TradeNotExist;
                return false;
            }

            foreach (var orderItem in order.Items)
            {
                if (!seller.TradeAbility.Inventory.IsPackageSellable(orderItem.SlotIndex, orderItem.PackageId))
                {
                    failureReason = TradePresellFailureReason.CantSell;
                    return false;
                }

                if (seller.TradeAbility.Inventory.IsSellableInventoryLimit(orderItem.SlotIndex, out var limitNumber))
                {
                    if (orderItem.Number <= limitNumber) continue;
                    failureReason = TradePresellFailureReason.UnderStock;
                    return false;
                }
            }

            return true;
        }

        public bool Prepay(CharacterObject payer, TradeOrder order, out TradePrepayFailureReason failureReason)
        {
            failureReason = TradePrepayFailureReason.Unknown;
            if (!payer.TradeAbility || payer.TradeAbility.Inventory == null)
            {
                return false;
            }

            if (_ongoingTrades.All(trade => trade.SerialNumber != order.TradeSerialNumber))
            {
                failureReason = TradePrepayFailureReason.TradeNotExist;
                return false;
            }

            foreach (var orderItem in order.Items)
            {
                if (!payer.TradeAbility.Inventory.IsPackagePayable(orderItem.SlotIndex, orderItem.PackageId))
                {
                    failureReason = TradePrepayFailureReason.CantPay;
                    return false;
                }

                if (payer.TradeAbility.Inventory.IsPayableInventoryLimit(orderItem.SlotIndex, out var limitNumber))
                {
                    if (orderItem.Number <= limitNumber) continue;
                    failureReason = TradePrepayFailureReason.ExceedDemand;
                    return false;
                }
            }

            if (order.TotalMoney > payer.TradeAbility.Money)
            {
                failureReason = TradePrepayFailureReason.MoneyNotEnough;
                return false;
            }

            return true;
        }

        public bool Sell(CharacterObject seller, TradeOrder order)
        {
            if (!seller.TradeAbility || seller.TradeAbility.Inventory == null)
            {
                return false;
            }

            if (_ongoingTrades.All(trade => trade.SerialNumber != order.TradeSerialNumber))
            {
                return false;
            }

            var inventoryChangedNumber = order.Items.Count(item =>
            {
                // 如果影响销售库存，则返回true
                seller.TradeAbility.Inventory.SellInventory(item.SlotIndex, item.Number, out var changed);
                return changed;
            });
            seller.TradeAbility.Money += order.TotalMoney;
            seller.TradeAbility.OnSell(order);
            if (inventoryChangedNumber > 0)
            {
                // 触发所有该角色参与的交易清单的刷新
                _ongoingTrades.ForEach(trade =>
                {
                    if (trade.ContainsCharacter(seller))
                    {
                        trade.SetManifest(GenerateManifest(trade.A, trade.B));
                        OnTradeManifestChanged?.Invoke(trade);
                    }
                });
            }

            return true;
        }

        public bool Pay(CharacterObject payer, TradeOrder order)
        {
            if (!payer.TradeAbility || payer.TradeAbility.Inventory == null)
            {
                return false;
            }

            if (_ongoingTrades.All(trade => trade.SerialNumber != order.TradeSerialNumber))
            {
                return false;
            }

            var inventoryChangedNumber = order.Items.Count(item =>
            {
                // 如果影响购入库存，则返回true
                payer.TradeAbility.Inventory.PayInventory(item.SlotIndex, item.Number, out var changed);
                return changed;
            });
            payer.TradeAbility.Money -= order.TotalMoney;
            payer.TradeAbility.OnPay(order);
            if (inventoryChangedNumber > 0)
            {
                // 触发所有该角色参与的交易清单的刷新
                _ongoingTrades.ForEach(trade =>
                {
                    if (trade.ContainsCharacter(payer))
                    {
                        trade.SetManifest(GenerateManifest(trade.A, trade.B));
                        OnTradeManifestChanged?.Invoke(trade);
                    }
                });
            }

            return true;
        }

        public void FinishTrade(string serialNumber)
        {
            for (var i = _ongoingTrades.Count - 1; i >= 0; i--)
            {
                var trade = _ongoingTrades[i];
                // 结束对应的交易
                if (trade.SerialNumber == serialNumber)
                {
                    DebugUtil.LogCyan(
                        $"角色({trade.A.Parameters.DebugName})和角色({trade.B.Parameters.DebugName})之间结束交易({trade.SerialNumber})");
                    trade.Finish();
                    _ongoingTrades.RemoveAt(i);
                    OnTradeFinished?.Invoke(trade);
                    break;
                }
            }
        }

        public void ClearAllTrades()
        {
            _ongoingTrades.ForEach(trade =>
            {
                DebugUtil.LogCyan(
                    $"角色({trade.A.Parameters.DebugName})和角色({trade.B.Parameters.DebugName})之间结束交易({trade.SerialNumber})");
                trade.Finish();
                OnTradeFinished?.Invoke(trade);
            });
            _ongoingTrades.Clear();
        }

        private TradeManifest GenerateManifest(CharacterObject a, CharacterObject b)
        {
            var items = new List<TradeManifestItem>();
            items.AddRange(GenerateManifestItems(a, b));
            items.AddRange(GenerateManifestItems(b, a));
            return new TradeManifest
            {
                items = items,
            };
        }

        private List<TradeManifestItem> GenerateManifestItems(CharacterObject seller, CharacterObject payer)
        {
            var manifestItems = new List<TradeManifestItem>();
            if (seller.TradeAbility.Inventory.sellableInventories.Count == 0 ||
                payer.TradeAbility.Inventory.payableInventories.Count == 0)
            {
                return manifestItems;
            }

            // 整体清单列表以卖方为主，以买方为从
            // 遍历卖方的卖出槽
            seller.TradeAbility.Inventory.sellableInventories.Values.ForEach(sellableSlotInventory =>
            {
                // 判断该卖出槽是否对该买方可见，不可见就跳过后续
                if (!seller.TradeAbility.Rule.IsSellableSlotVisible(sellableSlotInventory.slotId, seller, payer))
                {
                    return;
                }

                // 获取该卖出槽的数据
                var sellerPriceStrategy =
                    seller.TradeAbility.Rule.GetSellableSlotPriceStrategy(sellableSlotInventory.slotId);
                var sellerPriceFluctuation = 1f;
                seller.TradeAbility.Rule.GetSellableSlotPriceFluctuation(sellableSlotInventory.slotId, seller, payer,
                    out sellerPriceFluctuation);
                var remainingInventory =
                    sellableSlotInventory.numberLimit ? sellableSlotInventory.number : int.MaxValue;
                // 遍历买方的买入槽
                foreach (var payableSlotInventory in payer.TradeAbility.Inventory.payableInventories.Values)
                {
                    // 如果买入槽的物品id与上文的卖出槽不同或者该买入槽不可见，就跳过后续
                    if (payableSlotInventory.packageId != sellableSlotInventory.packageId ||
                        !payer.TradeAbility.Rule.IsPayableSlotVisible(payableSlotInventory.slotId, payer, seller))
                    {
                        continue;
                    }

                    // 获取该买入槽的数据
                    var payerPriceFluctuation = 1f;
                    payer.TradeAbility.Rule.GetPayableSlotPriceFluctuation(payableSlotInventory.slotId, payer, seller,
                        out payerPriceFluctuation);
                    // 计算最终波动率
                    var priceFluctuation = sellerPriceFluctuation;
                    switch (sellerPriceStrategy)
                    {
                        case TradePriceStrategy.SellerOnly:
                        {
                            priceFluctuation = sellerPriceFluctuation;
                        }
                            break;
                        case TradePriceStrategy.BuyerOnly:
                        {
                            priceFluctuation = payerPriceFluctuation;
                        }
                            break;
                        case TradePriceStrategy.Average:
                        {
                            priceFluctuation = (sellerPriceFluctuation + payerPriceFluctuation) / 2f;
                        }
                            break;
                    }

                    // 如果该买入槽无数量限制，就直接在这里中断，否则继续判断剩余数量决定是否中断
                    if (!payableSlotInventory.numberLimit)
                    {
                        manifestItems.Add(new TradeManifestItem
                        {
                            packageId = sellableSlotInventory.packageId,
                            sellerSlotIndex = sellableSlotInventory.slotId,
                            payerSlotIndex = payableSlotInventory.slotId,
                            priceFluctuation = priceFluctuation,
                            inventoryLimited = sellableSlotInventory.numberLimit,
                            inventory = sellableSlotInventory.number,
                            seller = seller,
                            payer = payer
                        });
                        break;
                    }
                    else if (payableSlotInventory.number >= remainingInventory)
                    {
                        manifestItems.Add(new TradeManifestItem
                        {
                            packageId = sellableSlotInventory.packageId,
                            sellerSlotIndex = sellableSlotInventory.slotId,
                            payerSlotIndex = payableSlotInventory.slotId,
                            priceFluctuation = priceFluctuation,
                            inventoryLimited = true,
                            inventory = remainingInventory,
                            seller = seller,
                            payer = payer
                        });
                        break;
                    }
                    else
                    {
                        // 只有这里会继续遍历买入槽
                        remainingInventory -= payableSlotInventory.number;
                        manifestItems.Add(new TradeManifestItem
                        {
                            packageId = sellableSlotInventory.packageId,
                            sellerSlotIndex = sellableSlotInventory.slotId,
                            payerSlotIndex = payableSlotInventory.slotId,
                            priceFluctuation = priceFluctuation,
                            inventoryLimited = true,
                            inventory = payableSlotInventory.number,
                            seller = seller,
                            payer = payer
                        });
                    }
                }
            });
            return manifestItems;
        }

        public bool TryGetSellableSlotInventory(
            string inventoryId,
            string configurationId,
            int slotId,
            int packageId,
            out int inventory
        )
        {
            inventory = 0;
            if (_tradeInventoryArchives.TryGetValue(inventoryId, out var tradeInventoryArchiveData))
            {
                if (tradeInventoryArchiveData.configurationId == configurationId &&
                    tradeInventoryArchiveData.sellableInventories.TryGetValue(slotId.ToString(),
                        out var tradeInventorySlotArchiveData))
                {
                    if (tradeInventorySlotArchiveData.packageId == packageId)
                    {
                        inventory = tradeInventorySlotArchiveData.inventory;
                        return true;
                    }
                }
            }

            return false;
        }

        public bool TryGetPayableSlotInventory(
            string inventoryId,
            string configurationId,
            int slotId,
            int packageId,
            out int inventory
        )
        {
            inventory = 0;
            if (_tradeInventoryArchives.TryGetValue(inventoryId, out var tradeInventoryArchiveData))
            {
                if (tradeInventoryArchiveData.configurationId == configurationId &&
                    tradeInventoryArchiveData.payableInventories.TryGetValue(slotId.ToString(),
                        out var tradeInventorySlotArchiveData))
                {
                    if (tradeInventorySlotArchiveData.packageId == packageId)
                    {
                        inventory = tradeInventorySlotArchiveData.inventory;
                        return true;
                    }
                }
            }

            return false;
        }

        public void RecordSellableSlotInventory(
            string inventoryId,
            string configurationId,
            int slotId,
            int packageId,
            int inventory
        )
        {
            if (_tradeInventoryArchives.TryGetValue(inventoryId, out var tradeInventoryArchiveData))
            {
                if (tradeInventoryArchiveData.sellableInventories.TryGetValue(slotId.ToString(),
                        out var tradeInventorySlotArchiveData))
                {
                    tradeInventorySlotArchiveData.slotId = slotId;
                    tradeInventorySlotArchiveData.packageId = packageId;
                    tradeInventorySlotArchiveData.inventory = inventory;
                }
                else
                {
                    tradeInventoryArchiveData.sellableInventories.Add(slotId.ToString(),
                        new TradeInventorySlotArchiveData
                        {
                            slotId = slotId,
                            packageId = packageId,
                            inventory = inventory
                        });
                }
            }
            else
            {
                _tradeInventoryArchives.Add(inventoryId, new TradeInventoryArchiveData
                {
                    id = inventoryId,
                    configurationId = configurationId,
                    sellableInventories = new Dictionary<string, TradeInventorySlotArchiveData>
                    {
                        [slotId.ToString()] = new TradeInventorySlotArchiveData
                        {
                            slotId = slotId,
                            packageId = packageId,
                            inventory = inventory
                        }
                    },
                    payableInventories = new Dictionary<string, TradeInventorySlotArchiveData>(),
                });
            }
        }

        public void RecordPayableSlotInventory(
            string inventoryId,
            string configurationId,
            int slotId,
            int packageId,
            int inventory
        )
        {
            if (_tradeInventoryArchives.TryGetValue(inventoryId, out var tradeInventoryArchiveData))
            {
                if (tradeInventoryArchiveData.payableInventories.TryGetValue(slotId.ToString(),
                        out var tradeInventorySlotArchiveData))
                {
                    tradeInventorySlotArchiveData.slotId = slotId;
                    tradeInventorySlotArchiveData.packageId = packageId;
                    tradeInventorySlotArchiveData.inventory = inventory;
                }
                else
                {
                    tradeInventoryArchiveData.payableInventories.Add(slotId.ToString(),
                        new TradeInventorySlotArchiveData
                        {
                            slotId = slotId,
                            packageId = packageId,
                            inventory = inventory
                        });
                }
            }
            else
            {
                _tradeInventoryArchives.Add(inventoryId, new TradeInventoryArchiveData
                {
                    id = inventoryId,
                    configurationId = configurationId,
                    sellableInventories = new Dictionary<string, TradeInventorySlotArchiveData>(),
                    payableInventories = new Dictionary<string, TradeInventorySlotArchiveData>
                    {
                        [slotId.ToString()] = new TradeInventorySlotArchiveData
                        {
                            slotId = slotId,
                            packageId = packageId,
                            inventory = inventory
                        }
                    },
                });
            }
        }
    }
}