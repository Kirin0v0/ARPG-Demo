using System;
using System.Collections.Generic;
using UnityEngine.Serialization;

namespace Trade.Data
{
    /// <summary>
    /// 交易库存存档数据类
    /// </summary>
    [Serializable]
    public class TradeInventoryArchiveData
    {
        public string id = "";
        public string configurationId = "";
        public Dictionary<string, TradeInventorySlotArchiveData> sellableInventories = new();
        public Dictionary<string, TradeInventorySlotArchiveData> payableInventories = new();
    }

    [Serializable]
    public class TradeInventorySlotArchiveData
    {
        public int slotId = -1;
        public int packageId = -1;
        public int inventory = 0;
    }
}