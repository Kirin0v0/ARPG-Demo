using System;
using System.Collections.Generic;
using Framework.DataStructure;

namespace Map.Data
{
    [Serializable]
    public class MapArchiveData
    {
        public Dictionary<string, MapItemArchiveData> maps = new();
        public MapPinArchiveData pin = new();
    }

    [Serializable]
    public class MapItemArchiveData
    {
        public int id = -1;
        public List<int> interactedPackages = new();
        public Dictionary<string, MapTradeArchiveData> trades = new();
    }

    [Serializable]
    public class MapTradeArchiveData
    {
        public int ownerId = -1;
        public string tradeId = "";
        public Dictionary<string, MapTradeSlotArchiveData> sellableSlots = new();
        public Dictionary<string, MapTradeSlotArchiveData> payableSlots = new();
    }

    [Serializable]
    public class MapTradeSlotArchiveData
    {
        public int slotId = -1;
        public int packageId = -1;
        public int inventory = 0;
    }

    [Serializable]
    public class MapPinArchiveData
    {
        public string mapId = "";
        public SerializableVector3 position = new SerializableVector3(0, 0, 0);
    }
}