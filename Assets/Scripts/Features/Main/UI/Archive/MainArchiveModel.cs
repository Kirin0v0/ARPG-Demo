using System;
using System.Collections.Generic;
using System.Linq;
using Archive;
using Archive.Data;
using Features.Main.Data;
using Framework.Core.LiveData;
using Map.Data;
using Quest;
using Quest.Config;
using UnityEngine;

namespace Features.Main.Archive
{
    public class MainArchiveModel
    {
        private readonly ArchiveManager _archiveManager;
        private readonly MapInfoContainer _mapInfoContainer;

        private readonly MutableLiveData<List<object>> _archiveSlots = new();
        public LiveData<List<object>> GetArchiveSlotList() => _archiveSlots;

        public MainArchiveModel(
            ArchiveManager archiveManager,
            MapInfoContainer mapInfoContainer
        )
        {
            _archiveManager = archiveManager;
            _mapInfoContainer = mapInfoContainer;
        }

        public void FetchArchiveSlotList(int count)
        {
            count = Mathf.Max(count, 0);
            var list = new List<object>();
            list.AddRange(_archiveManager.ArchiveSlots.Take(count).Select((archiveSlot, index) =>
                new MainArchiveItemUIData
                {
                    Id = archiveSlot.ID,
                    Auto = archiveSlot.Auto,
                    SaveTime = GetSaveTimeString(archiveSlot.SaveTimestamp),
                    PlayTime = GetPlayTimeString(archiveSlot.PlayTime),
                    PlayerName = archiveSlot.PlayerName,
                    PlayerLevel = archiveSlot.PlayerLevel,
                    MapName = GetMapName(archiveSlot.MapId),
                    QuestName = GetQuestName(archiveSlot.QuestId)
                }).ToList());
            while (list.Count < count)
            {
                list.Add(new MainArchivePlaceholderUIData());
            }

            _archiveSlots.SetValue(list);

            return;

            string GetSaveTimeString(long timestamp)
            {
                return DateTimeOffset.FromUnixTimeMilliseconds(timestamp).DateTime.ToLocalTime()
                    .ToString("yyyy/MM/dd");
            }

            string GetPlayTimeString(float playTime)
            {
                var hours = (int)playTime / 3600;
                var minutes = (int)(playTime % 3600) / 60;
                var seconds = (int)playTime % 60;
                return $"{hours:00}:{minutes:00}:{seconds:00}";
            }

            string GetMapName(int mapId)
            {
                return _mapInfoContainer.Data.TryGetValue(mapId, out var mapInfoData) ? mapInfoData.Name : "未知地图";
            }

            string GetQuestName(string questId)
            {
                if (string.IsNullOrEmpty(questId))
                {
                    return  "";
                }
                
                // return _questPool.TryGetQuestInfo(questId, out var questInfo) ? questInfo.title : "";
                return "";
            }
        }

        public bool SelectPreviousItem(int position, out int index, out object data)
        {
            index = -1;
            data = null;
            if (position < 0 || position >= _archiveSlots.Value.Count || position - 1 < 0)
            {
                return false;
            }

            index = position - 1;
            data = _archiveSlots.Value[index];
            switch (data)
            {
                case MainArchiveItemUIData itemData:
                {
                    itemData.Selected = true;
                }
                    break;
                case MainArchivePlaceholderUIData placeholderData:
                {
                    placeholderData.Selected = true;
                }
                    break;
                default:
                    return false;
            }

            return true;
        }

        public bool SelectNextItem(int position, out int index, out object data)
        {
            index = -1;
            data = null;
            if (position < 0 || position >= _archiveSlots.Value.Count || position + 1 >= _archiveSlots.Value.Count)
            {
                return false;
            }

            index = position + 1;
            data = _archiveSlots.Value[index];
            switch (data)
            {
                case MainArchiveItemUIData itemData:
                {
                    itemData.Selected = true;
                }
                    break;
                case MainArchivePlaceholderUIData placeholderData:
                {
                    placeholderData.Selected = true;
                }
                    break;
                default:
                    return false;
            }

            return true;
        }
    }
}