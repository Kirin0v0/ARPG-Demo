using System;
using System.Linq;
using Common;
using Features.Game.Data;
using Framework.Common.Debug;
using Framework.Core.LiveData;
using Humanoid.Data;
using Humanoid.Weapon;
using Humanoid.Weapon.SO;
using Package;
using Package.Data;
using Package.Data.Extension;
using Trade;
using Trade.Runtime;
using UnityEngine;

namespace Features.Game.UI.Trade
{
    public enum GameTradeMode
    {
        Payment,
        Sale,
    }

    public enum GameTradeTab
    {
        All,
        Weapon,
        Gear,
        Item,
        Material,
    }

    public class GameTradeModel
    {
        private readonly MutableLiveData<GameTradeMode> _mode = new();
        public LiveData<GameTradeMode> GetMode() => _mode;

        private readonly MutableLiveData<GameTradeTab> _tab = new();
        public LiveData<GameTradeTab> GetTab() => _tab;

        private readonly MutableLiveData<GameTradePageUIData> _page = new();
        public LiveData<GameTradePageUIData> GetPage() => _page;

        private readonly global::Trade.Runtime.Trade _trade;
        private readonly TradeManager _tradeManager;
        private readonly GameManager _gameManager;
        private readonly PackageManager _packageManager;
        private readonly HumanoidWeaponManager _weaponManager;

        public GameTradeModel(global::Trade.Runtime.Trade trade, TradeManager tradeManager, GameManager gameManager,
            PackageManager packageManager, HumanoidWeaponManager weaponManager)
        {
            _trade = trade;
            _tradeManager = tradeManager;
            _gameManager = gameManager;
            _packageManager = packageManager;
            _weaponManager = weaponManager;
            // 监听交易清单数据变化事件
            _tradeManager.OnTradeManifestChanged += HandleManifestChanged;
            // 自身监听内部数据变化来更新商品列表
            _mode.ObserveForever(HandleModeChanged);
            _tab.ObserveForever(HandleTabChanged);
        }

        public void Destroy()
        {
            // 清空页面
            _page.SetValue(new GameTradePageUIData
            {
                Goods = new(),
                Seller = null,
                Payer = null,
            });
            // 解除监听交易清单数据变化事件
            _tradeManager.OnTradeManifestChanged -= HandleManifestChanged;
            // 解除监听内部数据变化来更新商品列表
            _mode.RemoveObserver(HandleModeChanged);
            _tab.RemoveObserver(HandleTabChanged);
        }

        public void SwitchMode(GameTradeMode mode)
        {
            _mode.SetValue(mode);
        }

        public void SwitchTab(GameTradeTab tab)
        {
            _tab.SetValue(tab);
        }

        public void SwitchToPreviousTab()
        {
            var previousValue = (int)_tab.Value - 1;
            if (previousValue < 0)
            {
                previousValue = Enum.GetValues(typeof(GameTradeTab)).Length - 1; // 回到最后一个枚举值
            }

            SwitchTab((GameTradeTab)previousValue);
        }

        public void SwitchToNextTab()
        {
            var nextValue = (int)_tab.Value + 1;
            if (nextValue >= Enum.GetValues(typeof(GameTradeTab)).Length)
            {
                nextValue = 0; // 回到第一个枚举值
            }

            SwitchTab((GameTradeTab)nextValue);
        }

        public bool FocusUpperGoods(int position, out int index, out GameTradeGoodsUIData data)
        {
            index = -1;
            data = null;
            if (position < 0 || position >= _page.Value.Goods.Count || position - 1 < 0)
            {
                return false;
            }

            index = position - 1;
            data = _page.Value.Goods[index];
            data.Focused = true;
            return true;
        }

        public bool FocusLowerGoods(int position, out int index, out GameTradeGoodsUIData data)
        {
            index = -1;
            data = null;
            if (position < 0 || position >= _page.Value.Goods.Count || position + 1 >= _page.Value.Goods.Count)
            {
                return false;
            }

            index = position + 1;
            data = _page.Value.Goods[index];
            data.Focused = true;
            return true;
        }

        public bool IncreaseGoodsNumber(int position, int number, out GameTradeGoodsUIData data)
        {
            data = null;
            if (!_page.HasValue() || position < 0 || position >= _page.Value.Goods.Count)
            {
                return false;
            }

            data = _page.Value.Goods[position];
            data.IncreaseTargetNumber(number);
            return true;
        }

        public bool DecreaseGoodsNumber(int position, int number, out GameTradeGoodsUIData data)
        {
            data = null;
            if (!_page.HasValue() || position < 0 || position >= _page.Value.Goods.Count)
            {
                return false;
            }

            data = _page.Value.Goods[position];
            data.DecreaseTargetNumber(number);
            return true;
        }

        private void HandleManifestChanged(global::Trade.Runtime.Trade trade)
        {
            if (trade == _trade)
            {
                UpdateGoodsList();
            }
        }

        private void HandleModeChanged(GameTradeMode mode)
        {
            UpdateGoodsList();
        }

        private void HandleTabChanged(GameTradeTab tab)
        {
            UpdateGoodsList();
        }

        private void UpdateGoodsList()
        {
            if (!_mode.HasValue() || !_tab.HasValue())
            {
                return;
            }

            var mode = _mode.Value;
            var tab = _tab.Value;
            var self = _gameManager.Player;
            var other = _trade.A == self ? _trade.B : _trade.A;
            // 先过滤页面再过滤标签，得出最终清单
            var manifestItems = mode switch
            {
                GameTradeMode.Payment => _trade.Manifest.items.Where(item => item.payer == self).ToList(),
                GameTradeMode.Sale => _trade.Manifest.items.Where(item => item.seller == self).ToList(),
                _ => _trade.Manifest.items.Where(item => item.payer == self).ToList(),
            };
            manifestItems = manifestItems.Where(FilterManifestItemType).ToList();
            // 根据页面模式得出买方和卖方
            var seller = mode switch
            {
                GameTradeMode.Payment => other,
                GameTradeMode.Sale => self,
                _ => other,
            };
            var payer = mode switch
            {
                GameTradeMode.Payment => self,
                GameTradeMode.Sale => other,
                _ => self,
            };
            if (manifestItems.Count == 0)
            {
                _page.SetValue(new GameTradePageUIData
                {
                    Goods = new(),
                    Seller = seller,
                    Payer = payer,
                });
                return;
            }

            var goods = manifestItems
                .Select((slotData, index) => ToTradeGoodsUIData(slotData, false))
                .ToList();
            _page.SetValue(new GameTradePageUIData
            {
                Goods = goods,
                Seller = seller,
                Payer = payer,
            });

            return;

            bool FilterManifestItemType(TradeManifestItem manifestItem)
            {
                var packageInfoContainer =
                    GameApplication.Instance.ExcelBinaryManager.GetContainer<PackageInfoContainer>();
                if (!packageInfoContainer.Data.TryGetValue(manifestItem.packageId, out var packageInfoData))
                {
                    DebugUtil.LogWarning(
                        $"The PackageInfoData whose id is {manifestItem.packageId} is not existed in PackageInfoContainer");
                    return false;
                }

                return tab switch
                {
                    GameTradeTab.Weapon => packageInfoData.GetPackageType() == PackageType.Weapon,
                    GameTradeTab.Gear => packageInfoData.GetPackageType() == PackageType.Gear,
                    GameTradeTab.Item => packageInfoData.GetPackageType() == PackageType.Item,
                    GameTradeTab.Material => packageInfoData.GetPackageType() == PackageType.Material,
                    GameTradeTab.All => true,
                    _ => true,
                };
            }

            GameTradeGoodsUIData ToTradeGoodsUIData(TradeManifestItem manifestItem, bool focused)
            {
                var packageInfoContainer =
                    GameApplication.Instance.ExcelBinaryManager.GetContainer<PackageInfoContainer>();
                var packageInfoData = packageInfoContainer.Data[manifestItem.packageId];
                var weaponInfoContainer = GameApplication.Instance.ExcelBinaryManager
                    .GetContainer<HumanoidAppearanceWeaponInfoContainer>();
                var gearInfoContainer = GameApplication.Instance.ExcelBinaryManager
                    .GetContainer<HumanoidAppearanceGearInfoContainer>();
                return new GameTradeGoodsUIData
                {
                    PackageData = packageInfoData.ToPackageData(
                        HumanoidWeaponSingletonConfigSO.Instance.GetWeaponEquipmentConfiguration,
                        _weaponManager.GetWeaponAttackConfiguration,
                        _weaponManager.GetWeaponDefenceConfiguration,
                        weaponInfoContainer,
                        gearInfoContainer
                    ),
                    SellerSlotIndex = manifestItem.sellerSlotIndex,
                    PayerSlotIndex = manifestItem.payerSlotIndex,
                    PackageName = packageInfoData.Name,
                    UnitPrice = Mathf.RoundToInt(packageInfoData.Price * manifestItem.priceFluctuation),
                    TargetNumber = 0,
                    InventoryLimited = manifestItem.inventoryLimited,
                    Inventory = manifestItem.inventory,
                    HoldNumber = _packageManager.GetPackageCount(manifestItem.packageId),
                    Focused = focused,
                };
            }
        }
    }
}