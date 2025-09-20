namespace Archive.Data
{
    /// <summary>
    /// 存档槽数据，仅用于展示
    /// </summary>
    public struct ArchiveSlotData
    {
        public int ID;
        public bool Auto;
        public long SaveTimestamp;
        public float PlayTime;
        public string PlayerName;
        public int PlayerLevel;
        public int MapId;
        public string QuestId;
    }
}