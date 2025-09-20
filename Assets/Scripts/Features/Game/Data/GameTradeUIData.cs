using System.Collections.Generic;
using Character;
using Package.Data;
using UnityEngine;

namespace Features.Game.Data
{
    public class GameTradePageUIData
    {
        public List<GameTradeGoodsUIData> Goods = new();
        public CharacterObject Seller;
        public CharacterObject Payer;
    }
    
    public class GameTradeGoodsUIData
    {
        public PackageData PackageData;
        public int SellerSlotIndex;
        public int PayerSlotIndex;
        public string PackageName;
        public float UnitPrice;
        public int TargetNumber;
        public bool InventoryLimited;
        public int Inventory;
        public int HoldNumber;
        public bool Focused;

        public bool Available => !InventoryLimited || Inventory > 0;
        public bool AllowIncreaseTargetNumber => !InventoryLimited || TargetNumber < Inventory;
        public bool AllowDecreaseTargetNumber => TargetNumber > 0;
        public int TotalMoney => Mathf.RoundToInt(UnitPrice * TargetNumber);

        public void IncreaseTargetNumber(int number)
        {
            if (!AllowIncreaseTargetNumber)
            {
                return;
            }

            TargetNumber = InventoryLimited
                ? Mathf.Clamp(TargetNumber + number, 0, Inventory)
                : Mathf.Max(0, TargetNumber + number);
        }

        public void DecreaseTargetNumber(int number)
        {
            if (!AllowDecreaseTargetNumber)
            {
                return;
            }

            TargetNumber = InventoryLimited
                ? Mathf.Clamp(TargetNumber - number, 0, Inventory)
                : Mathf.Max(0, TargetNumber - number);
        }
    }
}