using Map;
using Package.Data;
using Sirenix.Utilities;
using Trade;
using Trade.Config;
using Trade.Data;
using Trade.Runtime;
using UnityEngine;
using VContainer;

namespace Character.Ability
{
    public class CharacterTradeMerchantAbility : CharacterTradeAbility
    {
        [Inject] private MapManager _mapManager;
        [Inject] private TradeManager _tradeManager;

        private string _tradeId;

        public override void Bind(TradeConfig tradeConfig)
        {
            if (tradeConfig == null)
            {
                _tradeId = $"{_mapManager.MapId}_{Owner.Parameters.id}_trade";
                Inventory = new();
                Rule = new();
                return;
            }

            _tradeId = $"{_mapManager.MapId}_{Owner.Parameters.id}_{tradeConfig.GetInstanceID()}";
            // 先根据交易配置生成库存和规则
            Inventory = tradeConfig.GetTradeInventory(_tradeId,
                GameApplication.Instance.ExcelBinaryManager.GetContainer<PackageInfoContainer>());
            Rule = tradeConfig.GetTradeRule(_tradeId);
            // 根据持久化数据设置库存
            Inventory.sellableInventories.Values.ForEach(slotInventory =>
            {
                if (_tradeManager.TryGetSellableSlotInventory(
                        Inventory.id,
                        Inventory.configurationId,
                        slotInventory.slotId,
                        slotInventory.packageId,
                        out var inventory))
                {
                    slotInventory.number = inventory;
                }
            });
            Inventory.payableInventories.Values.ForEach(slotInventory =>
            {
                if (_tradeManager.TryGetPayableSlotInventory(
                        Inventory.id,
                        Inventory.configurationId,
                        slotInventory.slotId,
                        slotInventory.packageId,
                        out var inventory))
                {
                    slotInventory.number = inventory;
                }
            });
        }

        public override void Tick(float deltaTime)
        {
            // 保证商人永远可以有足够金钱进行交易
            Money = 1000000;
        }

        public override void OnSell(TradeOrder order)
        {
            base.OnSell(order);
            // 记录可售出物品库存
            Inventory.sellableInventories.Values.ForEach(slotInventory =>
            {
                if (!slotInventory.numberLimit)
                {
                    return;
                }

                _tradeManager.RecordSellableSlotInventory(Inventory.id, Inventory.configurationId, slotInventory.slotId,
                    slotInventory.packageId, slotInventory.number);
            });
        }

        public override void OnPay(TradeOrder order)
        {
            base.OnPay(order);
            // 记录可购入物品库存
            Inventory.payableInventories.Values.ForEach(slotInventory =>
            {
                if (!slotInventory.numberLimit)
                {
                    return;
                }

                _tradeManager.RecordPayableSlotInventory(Inventory.id, Inventory.configurationId, slotInventory.slotId,
                    slotInventory.packageId, slotInventory.number);
            });
        }
    }
}