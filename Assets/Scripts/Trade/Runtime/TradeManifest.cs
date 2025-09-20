using System;
using System.Collections.Generic;
using Character;

namespace Trade.Runtime
{
    [Serializable]
    public class TradeManifest
    {
        public List<TradeManifestItem> items = new();
    }

    [Serializable]
    public class TradeManifestItem
    {
        public int packageId;
        public int sellerSlotIndex;
        public int payerSlotIndex;
        public float priceFluctuation;
        public bool inventoryLimited;
        public int inventory;
        public CharacterObject seller;
        public CharacterObject payer;
    }
}