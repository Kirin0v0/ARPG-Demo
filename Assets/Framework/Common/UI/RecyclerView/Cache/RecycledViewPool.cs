using System.Collections.Generic;
using UnityEngine;

namespace Framework.Common.UI.RecyclerView.Cache
{
    public class RecycledViewPool
    {
        private const int MaxScrap = 5;

        private class ScrapData
        {
            public readonly Stack<RecyclerViewHolder> ScrapHeap = new();
            public int MaxScrap;
        }

        private readonly Dictionary<int, ScrapData> _scraps = new(); // key为viewType，value为碎片数据的字典

        public void SetMaxRecycledViews(int viewType, int max)
        {
            var scrapData = GetScrapDataForType(viewType);
            scrapData.MaxScrap = max;
            // 删除多余ViewHolder
            while (scrapData.ScrapHeap.Count > max)
            {
                if (scrapData.ScrapHeap.TryPop(out var viewHolder))
                {
                    GameObject.Destroy(viewHolder.gameObject);
                }
            }
        }

        public RecyclerViewHolder GetRecycledViewHolder(int viewType, bool isRemovedWhenGet)
        {
            // 这里不通过GetScrapDataForType函数，避免不存在缓存时凭空创建缓存
            if (_scraps.TryGetValue(viewType, out var scrapData) && scrapData.ScrapHeap.Count != 0)
            {
                return isRemovedWhenGet ? scrapData.ScrapHeap.Pop() : scrapData.ScrapHeap.Peek();
            }

            return null;
        }

        public bool IsViewHolderRecyclable(RecyclerViewHolder viewHolder)
        {
            var scrapData = GetScrapDataForType(viewHolder.ViewType);
            return scrapData.MaxScrap > scrapData.ScrapHeap.Count;
        }

        public void PutRecycledViewHolder(RecyclerViewHolder viewHolder)
        {
            var scrapData = GetScrapDataForType(viewHolder.ViewType);
            if (scrapData.MaxScrap <= scrapData.ScrapHeap.Count)
            {
                GameObject.Destroy(viewHolder.gameObject);
                return;
            }

            viewHolder.Reset();
            scrapData.ScrapHeap.Push(viewHolder);
        }

        public void Clear()
        {
            foreach (var scrapData in _scraps.Values)
            {
                while (scrapData.ScrapHeap.TryPop(out var viewHolder))
                {
                    GameObject.Destroy(viewHolder.gameObject);
                }
            }
        }

        private ScrapData GetScrapDataForType(int viewType)
        {
            if (!_scraps.TryGetValue(viewType, out var scrapData))
            {
                scrapData = new ScrapData
                {
                    MaxScrap = MaxScrap
                };
                _scraps.Add(viewType, scrapData);
            }

            return scrapData;
        }
    }
}