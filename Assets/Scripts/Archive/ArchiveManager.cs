using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Archive.Data;
using Archive.Security;
using Framework.Common.Debug;
using Framework.Common.Util;
using Quest.Data;
using UnityEngine;

namespace Archive
{
    /// <summary>
    /// 存档管理类
    /// </summary>
    public class ArchiveManager
    {
        // 存档槽列表，从新到旧排序
        public List<ArchiveSlotData> ArchiveSlots
        {
            get
            {
                var archiveSlots = _archives.Select(tuple => new ArchiveSlotData
                {
                    ID = tuple.archiveData.id,
                    Auto = tuple.archiveData.auto,
                    SaveTimestamp = tuple.archiveData.archiveTimestamp,
                    PlayTime = tuple.archiveData.playTime,
                    PlayerName = tuple.archiveData.player.name,
                    PlayerLevel = tuple.archiveData.player.level,
                    MapId = tuple.archiveData.player.map.id,
                    QuestId = tuple.archiveData.quest.quests.FirstOrDefault(quest => quest.state.IsQuestInProgress())
                        ?.id ?? "",
                }).ToList();
                return archiveSlots;
            }
        }

        private const string ArchiveFileExtension = "sav";

        private readonly List<IArchivable> _archivableImpls = new();
        private readonly List<(string filePath, ArchiveData archiveData)> _archives = new();

        private readonly string _archiveFolderPath;
        private readonly int _maxArchiveSlots;
        private readonly ISecurityStrategy _securityStrategy;

        // 最新未存档使用的id
        private int _newestUnusedArchiveId = 1;

        private float _playTimeAfterLoad = 0f;

        public ArchiveManager(string archiveFolderPath, int maxArchiveSlots, ISecurityStrategy securityStrategy)
        {
            _archiveFolderPath = archiveFolderPath;
            _maxArchiveSlots = maxArchiveSlots;
            _securityStrategy = securityStrategy;
            Init();
        }

        public void UpdatePlayTime(float deltaTime)
        {
            _playTimeAfterLoad += deltaTime;
        }

        public ArchiveData GetArchive(int archiveId)
        {
            return _archives.Find(tuple => tuple.archiveData.id == archiveId).archiveData;
        }

        public ArchiveData GetNewestArchive()
        {
            return _archives.Count > 0 ? _archives[0].archiveData : null;
        }

        public int SaveNewArchive(ArchiveData archiveData)
        {
            archiveData.id = _newestUnusedArchiveId;
            archiveData.archiveTimestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            SaveArchiveInternal(archiveData);
            return archiveData.id;
        }

        public void DeleteArchive(int archiveId)
        {
            var index = _archives.FindIndex(tuple => tuple.archiveData.id == archiveId);
            if (index != -1)
            {
                var tuple = _archives[index];

                // 删除存档文件
                File.Delete(tuple.filePath);

                // 删除内存中的存档数据
                _archives.RemoveAt(index);
            }
        }

        public void Register(IArchivable archivableImpl)
        {
            _archivableImpls.Add(archivableImpl);
        }

        public void Unregister(IArchivable archivableImpl)
        {
            _archivableImpls.Remove(archivableImpl);
        }

        public void NotifySave(bool auto)
        {
            // 创建新存档数据，然后再通知存档接口修改数据
            var archiveData = new ArchiveData
            {
                id = _newestUnusedArchiveId,
                auto = auto,
                playTime = _playTimeAfterLoad,
                archiveTimestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds()
            };
            _archivableImpls.ForEach(archivableImpl => { archivableImpl.Save(archiveData); });
            SaveArchiveInternal(archiveData);
        }

        public void NotifyLoad(int archiveId)
        {
            var archiveData = _archives.Find(tuple => tuple.archiveData.id == archiveId).archiveData;
            if (archiveData != null)
            {
                _archivableImpls.ForEach(archivableImpl =>
                {
                    DebugUtil.LogGrey($"{archivableImpl}加载前: {DateTimeOffset.Now.ToUnixTimeSeconds()}");
                    archivableImpl.Load(archiveData);
                    DebugUtil.LogGrey($"{archivableImpl}加载后: {DateTimeOffset.Now.ToUnixTimeSeconds()}");
                });
                _playTimeAfterLoad = archiveData.playTime;
            }
            else
            {
                DebugUtil.LogWarning($"The archive({archiveId}) is missing");
            }
        }

        public void NotifyNewestLoad()
        {
            var newestArchive = GetNewestArchive();
            if (newestArchive != null)
            {
                _archivableImpls.ForEach(archivableImpl => archivableImpl.Load(newestArchive));
                _playTimeAfterLoad = newestArchive.playTime;
            }
        }

        private void Init()
        {
            if (!Directory.Exists(_archiveFolderPath))
            {
                Directory.CreateDirectory(_archiveFolderPath);
            }

            var filePaths = Directory.GetFiles(_archiveFolderPath);
            var idSet = new HashSet<int>();
            foreach (var filePath in filePaths)
            {
                if (!filePath.EndsWith($".{ArchiveFileExtension}"))
                {
                    continue;
                }

                try
                {
                    var fileContent = File.ReadAllText(filePath);
                    var archiveData = JsonUtil.ToObject<ArchiveData>(_securityStrategy.Decrypt(fileContent),
                        JsonUtil.Strategy.LitJson);
                    // 过滤重复id的存档，保证id唯一
                    if (idSet.Add(archiveData.id))
                    {
                        _archives.Add((filePath, archiveData));
                        // 在初始化时同时设置最新存档id
                        if (_newestUnusedArchiveId < archiveData.id + 1)
                        {
                            _newestUnusedArchiveId = archiveData.id + 1;
                        }
                    }
                }
                catch (Exception e)
                {
                    throw new Exception(
                        $"The file({filePath}) can't match with the formatter of ArchiveData, error message: {e.Message}");
                }
            }

            // 最后将存档从id大到小排序
            _archives.Sort((data1, data2) => data1.archiveData.id >= data2.archiveData.id ? -1 : 1);
        }

        private void SaveArchiveInternal(ArchiveData archiveData)
        {
            // 删除最旧的存档数据
            if (_archives.Count > _maxArchiveSlots)
            {
                _archives.RemoveAt(_maxArchiveSlots - 1);
            }

            // 写入存档文件夹内
            if (!Directory.Exists(_archiveFolderPath))
            {
                Directory.CreateDirectory(_archiveFolderPath);
            }

            var archiveFilePath = _archiveFolderPath + "/" + archiveData.id + "_" + (archiveData.auto ? "auto_" : "") +
                                  archiveData.archiveTimestamp + "." + ArchiveFileExtension;
            var jsonString = JsonUtil.ToJson(archiveData, JsonUtil.Strategy.LitJson);
            File.WriteAllText(archiveFilePath, _securityStrategy.Encrypt(jsonString));

            // 这里立即反序列化json存档数据插入内存
            _archives.Insert(0,
                (archiveFilePath, JsonUtil.ToObject<ArchiveData>(jsonString, JsonUtil.Strategy.LitJson)));

            // 最新存档id+1
            _newestUnusedArchiveId++;
        }
    }
}