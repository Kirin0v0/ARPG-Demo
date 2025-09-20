using System;
using System.Collections.Generic;
using Trade.Data;
using UnityEngine;

namespace Trade.Runtime
{
    [Serializable]
    public class TradeInventory
    {
        public string id = "";
        public string configurationId = "";
        public Dictionary<int, TradeSlotInventory> sellableInventories = new();
        public Dictionary<int, TradeSlotInventory> payableInventories = new();

        public bool IsPackageSellable(int slotId, int packageId)
        {
            if (sellableInventories.TryGetValue(slotId, out var inventory))
            {
                return inventory.packageId == packageId;
            }

            return false;
        }

        public bool IsSellableInventoryLimit(int slotId, out int limitNumber)
        {
            if (sellableInventories.TryGetValue(slotId, out var inventory))
            {
                limitNumber = inventory.number;
                return inventory.numberLimit;
            }

            limitNumber = 0;
            return true;
        }

        public void SellInventory(int slotId, int number, out bool changedInventoryNumber)
        {
            changedInventoryNumber = false;
            if (sellableInventories.TryGetValue(slotId, out var inventory) && inventory.numberLimit)
            {
                inventory.number = Mathf.Clamp(inventory.number - number, 0, inventory.number);
                changedInventoryNumber = true;
            }
        }

        public bool IsPackagePayable(int slotId, int packageId)
        {
            if (payableInventories.TryGetValue(slotId, out var inventory))
            {
                return inventory.packageId == packageId;
            }

            return false;
        }

        public bool IsPayableInventoryLimit(int slotId, out int limitNumber)
        {
            if (payableInventories.TryGetValue(slotId, out var inventory))
            {
                limitNumber = inventory.number;
                return inventory.numberLimit;
            }

            limitNumber = 0;
            return true;
        }

        public void PayInventory(int slotId, int number, out bool changedInventoryNumber)
        {
            changedInventoryNumber = false;
            if (payableInventories.TryGetValue(slotId, out var inventory) && inventory.numberLimit)
            {
                inventory.number = Mathf.Clamp(inventory.number - number, 0, inventory.number);
                changedInventoryNumber = true;
            }
        }
    }

    [Serializable]
    public class TradeSlotInventory
    {
        public int slotId;
        public int packageId;
        public bool numberLimit;
        public int number; // 仅当存在数量限制时才会使用数量
    }
}