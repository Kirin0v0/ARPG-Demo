using System;
using System.Collections.Generic;
using Character.Data;
using Dialogue.Data;
using Map.Data;
using Package.Data;
using Player.Data;
using Quest.Data;
using UnityEngine.Serialization;

namespace Archive.Data
{
    /// <summary>
    /// 存档数据，用于存储本地以及从本地读取
    /// </summary>
    [Serializable]
    public class ArchiveData
    {
        // 仅存档相关，理论上存档管理器会自动填充内容
        public int id = -1;
        public bool auto = true;
        public float playTime = 0f;
        public long archiveTimestamp = 0;

        // 玩家角色相关
        public PlayerArchiveData player = new();

        // 背包相关
        public PackageArchiveData package = new();

        // 地图相关
        public MapArchiveData map = new();

        // 对话相关
        public DialogueArchiveData dialogue = new();

        // 任务相关
        public QuestArchiveData quest = new();
    }
}