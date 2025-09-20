using System;
using System.Collections.Generic;

namespace Trade.Runtime
{
    public struct TradeOrder
    {
        public string TradeSerialNumber;
        public List<TradeOrderItem> Items;
        public int TotalMoney;
    }

    public struct TradeOrderItem
    {
        public int SlotIndex;
        public int PackageId;
        public int Number;
        public int Money;
    }
}