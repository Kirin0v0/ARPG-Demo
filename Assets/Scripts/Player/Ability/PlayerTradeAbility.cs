using System.Collections.Generic;
using System.Linq;
using Character;
using Character.Ability;
using Character.Data.Extension;
using Humanoid;
using Humanoid.Data;
using Humanoid.Weapon;
using Humanoid.Weapon.SO;
using Package;
using Package.Data;
using Package.Data.Extension;
using Package.Runtime;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using Trade.Config;
using Trade.Config.PriceFluctuationRule;
using Trade.Config.VisibilityRule;
using Trade.Data;
using Trade.Runtime;
using UnityEngine;
using VContainer;

namespace Player.Ability
{
    public class PlayerTradeAbility : CharacterTradeAbility
    {
        private new PlayerCharacterObject Owner => base.Owner as PlayerCharacterObject;

        [Inject] private PackageManager _packageManager;
        [Inject] private PlayerDataManager _playerDataManager;
        [Inject] private HumanoidWeaponManager _weaponManager;

        protected override void OnInit()
        {
            base.OnInit();
            Money = _playerDataManager.Money;
            // 监听玩家物品列表变化
            _packageManager.OnPackageGroupAdded += HandlePackageGroupAdded;
            _packageManager.OnPackageGroupChanged += HandlePackageGroupChanged;
            _packageManager.OnPackageGroupRemoved += HandlePackageGroupRemoved;
            _packageManager.OnWeaponOrGearChanged += HandleWeaponOrGearChanged;
        }

        public override void Bind(TradeConfig tradeConfig)
        {
            // 这里无视传入数据，使用玩家数据构建交易数据
            BuildTradeByPlayerData();
        }

        public override void Tick(float deltaTime)
        {
            // 玩家金币和数据保持一致
            Money = _playerDataManager.Money;
        }

        protected override void OnDispose()
        {
            base.OnDispose();
            // 取消监听玩家物品列表变化
            _packageManager.OnPackageGroupAdded -= HandlePackageGroupAdded;
            _packageManager.OnPackageGroupChanged -= HandlePackageGroupChanged;
            _packageManager.OnPackageGroupRemoved -= HandlePackageGroupRemoved;
            _packageManager.OnWeaponOrGearChanged -= HandleWeaponOrGearChanged;
        }

        public override void OnSell(TradeOrder order)
        {
            base.OnSell(order);
            // 删除订单的物品列表
            order.Items.ForEach(item => { _packageManager.DeletePackage(item.PackageId, item.Number); });
            // 添加玩家金钱
            _playerDataManager.EarnMoney(order.TotalMoney, true);
        }

        public override void OnPay(TradeOrder order)
        {
            base.OnPay(order);
            // 添加订单的物品列表
            order.Items.ForEach(item => { _packageManager.AddPackage(item.PackageId, item.Number, false); });
            // 减少玩家金钱
            _playerDataManager.CostMoney(order.TotalMoney, true);
        }

        public void RebuildTrade()
        {
            BuildTradeByPlayerData();
        }

        private void HandlePackageGroupAdded(PackageGroup packageGroup)
        {
            BuildTradeByPlayerData();
        }

        private void HandlePackageGroupChanged(PackageGroup packageGroup)
        {
            BuildTradeByPlayerData();
        }

        private void HandlePackageGroupRemoved(PackageGroup packageGroup)
        {
            BuildTradeByPlayerData();
        }

        private void HandleWeaponOrGearChanged()
        {
            BuildTradeByPlayerData();
        }

        private void BuildTradeByPlayerData()
        {
            // 获取可出售物品和可出售规则列表
            var sellableInventories = new Dictionary<int, TradeSlotInventory>();
            var sellableRules = new Dictionary<int, TradeSellableSlotRule>();
            _packageManager.PackageGroups.ForEach((packageGroup, index) =>
            {
                // 装备物品不纳入统计
                if (_packageManager.IsGroupEquipped(packageGroup.GroupId))
                {
                    return;
                }

                if (sellableInventories.TryGetValue(packageGroup.Data.Id, out var tradeSlotData))
                {
                    tradeSlotData.number += packageGroup.Number;
                }
                else
                {
                    sellableInventories.Add(packageGroup.Data.Id, new TradeSlotInventory
                    {
                        slotId = packageGroup.Data.Id,
                        packageId = packageGroup.Data.Id,
                        numberLimit = true,
                        number = packageGroup.Number,
                    });
                }

                if (!sellableRules.TryGetValue(packageGroup.Data.Id, out var tradeSlotRule))
                {
                    sellableRules.Add(packageGroup.Data.Id, new TradeSellableSlotRule
                    {
                        slotId = packageGroup.Data.Id,
                        defaultPriceFluctuation = 1f,
                        visibilitySetters = new List<BaseTradeSlotVisibilityRule>(),
                        priceFluctuationCalculators = new List<BaseTradeSlotPriceFluctuationRule>(),
                        priceStrategy = TradePriceStrategy.BuyerOnly,
                    });
                }
            });
            // 获取可购入物品和可购入规则列表
            var payableInventories = new Dictionary<int, TradeSlotInventory>();
            var payableRules = new Dictionary<int, TradePayableSlotRule>();
            GameApplication.Instance.ExcelBinaryManager.GetContainer<PackageInfoContainer>().Data.Values.ForEach(
                (packageInfoData, index) =>
                {
                    // 这里玩家根据种族过滤物品限制
                    var packageData = packageInfoData.ToPackageData(
                        HumanoidWeaponSingletonConfigSO.Instance.GetWeaponEquipmentConfiguration,
                        _weaponManager.GetWeaponAttackConfiguration,
                        _weaponManager.GetWeaponDefenceConfiguration,
                        GameApplication.Instance.ExcelBinaryManager
                            .GetContainer<HumanoidAppearanceWeaponInfoContainer>(),
                        GameApplication.Instance.ExcelBinaryManager.GetContainer<HumanoidAppearanceGearInfoContainer>()
                    );
                    // 跳过非本种族的装备物品
                    if (packageData is PackageGearData packageGearData)
                    {
                        if (!packageGearData.Races.MatchRestriction(Owner.HumanoidParameters.race))
                        {
                            return;
                        }
                    }

                    switch (packageData.QuantitativeRestriction)
                    {
                        case PackageQuantitativeRestriction.NoRestriction: // 物品持有数量无限制则认为购买槽没有数量限制
                        {
                            payableInventories.Add(packageData.Id, new TradeSlotInventory
                            {
                                slotId = packageData.Id,
                                packageId = packageData.Id,
                                numberLimit = false,
                                number = 0,
                            });
                            payableRules.Add(packageData.Id, new TradePayableSlotRule
                            {
                                slotId = packageData.Id,
                                defaultPriceFluctuation = 1f,
                                visibilitySetters = new List<BaseTradeSlotVisibilityRule>(),
                                priceFluctuationCalculators = new List<BaseTradeSlotPriceFluctuationRule>(),
                            });
                        }
                            break;
                        case PackageQuantitativeRestriction.OnlyOneGroup: // 物品持有数量仅一组则认为购买槽有数量限制，且限制数量为允许持有数量减去当前持有数量
                        {
                            payableInventories.Add(packageData.Id, new TradeSlotInventory
                            {
                                slotId = packageData.Id,
                                packageId = packageData.Id,
                                numberLimit = true,
                                number = Mathf.Clamp(
                                    packageData.GroupMaximum - _packageManager.GetPackageCount(packageData.Id),
                                    0,
                                    packageData.GroupMaximum
                                ),
                            });
                            payableRules.Add(packageData.Id, new TradePayableSlotRule
                            {
                                slotId = packageData.Id,
                                defaultPriceFluctuation = 1f,
                                visibilitySetters = new List<BaseTradeSlotVisibilityRule>(),
                                priceFluctuationCalculators = new List<BaseTradeSlotPriceFluctuationRule>(),
                            });
                        }
                            break;
                    }
                });
            // 构建交易仓库
            Inventory = new TradeInventory
            {
                id = "Player",
                configurationId = "",
                sellableInventories = sellableInventories,
                payableInventories = payableInventories,
            };
            // 构建交易规则
            Rule = new TradeRule
            {
                id = "Player",
                configurationId = "",
                sellableRules = sellableRules,
                payableRules = payableRules,
            };
        }
    }
}