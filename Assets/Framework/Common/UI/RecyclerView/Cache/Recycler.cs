using System;
using System.Collections.Generic;
using Framework.Common.Debug;
using UnityEngine;

namespace Framework.Common.UI.RecyclerView.Cache
{
    public class Recycler : IRecyclerQuery
    {
        private const int MaxCacheSize = 2;

        private readonly List<RecyclerViewHolder> _visibleViewHolders = new(); // 可用子项列表
        private readonly List<RecyclerViewHolder> _cachedViewHolders = new(); // 缓存子项列表
        private readonly RecycledViewPool _recycledViewPool = new(); // 被回收子项池

        private readonly RecyclerView _recyclerView;

        private readonly int _maxCacheSize;

        public Recycler(RecyclerView recyclerView)
        {
            _recyclerView = recyclerView;
            _maxCacheSize = MaxCacheSize;
        }

        public RecyclerViewHolder[] GetVisibleViewHolders() => _visibleViewHolders.ToArray();

        public RecyclerViewHolder GetViewHolderForPosition(int position)
        {
            return TryGetViewHolderForPosition(position);
        }

        public void RecycleViewHolder(RecyclerViewHolder viewHolder)
        {
            RecycleViewHolderInternal(viewHolder);
        }

        public void MarkVisibleAndCachedViewHoldersNeedUpdate()
        {
            _visibleViewHolders.ForEach(viewHolder =>
                viewHolder.AddFlags(RecyclerViewHolder.FlagUpdate));
            _cachedViewHolders.ForEach(viewHolder =>
                viewHolder.AddFlags(RecyclerViewHolder.FlagUpdate));
        }

        public void ShowViewHolder(RecyclerViewHolder viewHolder)
        {
            if (!_visibleViewHolders.Contains(viewHolder))
            {
                viewHolder.gameObject.SetActive(true);
                _visibleViewHolders.Add(viewHolder);
                _recyclerView.Adapter?.ShowViewHolder(viewHolder);
            }
        }

        public void HideViewHolder(RecyclerViewHolder viewHolder)
        {
            if (_visibleViewHolders.Contains(viewHolder))
            {
                _visibleViewHolders.Remove(viewHolder);
                viewHolder.gameObject.SetActive(false);
                _recyclerView.Adapter?.HideViewHolder(viewHolder);
            }
        }

        public RecyclerViewHolder GetVisibleViewHolderForPosition(int position)
        {
            foreach (var viewHolder in _visibleViewHolders)
            {
                if (viewHolder.Position == position)
                {
                    return viewHolder;
                }
            }

            return null;
        }

        public RecyclerViewHolder GetVisibleOrCachedViewHolderForPosition(int position)
        {
            return GetVisibleOrCachedViewHolderForPosition(position, false);
        }

        public RecyclerViewHolder GetVisibleOrCachedViewHolderForPosition(int position, bool isRemovedWhenGet)
        {
            var visibleViewHolder = GetVisibleViewHolderForPosition(position);
            if (visibleViewHolder)
            {
                return visibleViewHolder;
            }

            for (var i = 0; i < _cachedViewHolders.Count; i++)
            {
                var cachedViewHolder = _cachedViewHolders[i];
                if (cachedViewHolder.Position == position)
                {
                    if (isRemovedWhenGet)
                    {
                        _cachedViewHolders.RemoveAt(i);
                    }

                    return cachedViewHolder;
                }
            }

            return null;
        }

        public void Clear()
        {
            ClearVisibleViewHolders();
            ClearCachedViewHolders();
            _recycledViewPool.Clear();

            void ClearVisibleViewHolders()
            {
                _visibleViewHolders.ForEach(viewHolder =>
                {
                    _recyclerView.Adapter?.HideViewHolder(viewHolder);
                    _recyclerView.Adapter?.DestroyViewHolder(viewHolder);
                });
                _visibleViewHolders.Clear();
            }

            void ClearCachedViewHolders()
            {
                _cachedViewHolders.ForEach(viewHolder => { _recyclerView.Adapter?.DestroyViewHolder(viewHolder); });
                _cachedViewHolders.Clear();
            }
        }

        public void HideAndRecyclerInvalidViewHolders()
        {
            var index = 0;
            while (index < _visibleViewHolders.Count)
            {
                var viewHolder = _visibleViewHolders[index];
                if (viewHolder.Position == RecyclerView.NoPosition)
                {
                    HideViewHolder(viewHolder);
                    RecycleViewHolder(viewHolder);
                }
                else
                {
                    index++;
                }
            }
        }

        private RecyclerViewHolder TryGetViewHolderForPosition(int position)
        {
            if (!_recyclerView.Adapter)
            {
                return null;
            }

            if (position < 0 || position >= _recyclerView.Adapter.GetItemCount())
            {
                return null;
            }

            var viewType = _recyclerView.Adapter.GetItemViewType(position);
            var viewHolder = GetVisibleOrCachedViewHolderForPosition(position, true);
            if (!viewHolder)
            {
                viewHolder = _recycledViewPool.GetRecycledViewHolder(viewType, true);
                if (viewHolder)
                {
                    viewHolder.Reset();
                }
                else
                {
                    viewHolder = _recyclerView.Adapter.CreateViewHolder(viewType);
                    if (viewHolder)
                    {
                        viewHolder.gameObject.transform.SetParent(_recyclerView.Content);
                        viewHolder.gameObject.transform.localPosition = Vector3.zero;
                        viewHolder.gameObject.transform.localRotation = Quaternion.identity;
                        viewHolder.gameObject.transform.localScale = Vector3.one;
                    }
                }
            }

            if (!viewHolder)
            {
                return null;
            }

            if (!viewHolder.IsBound() || viewHolder.NeedsUpdate())
            {
                TryBindViewHolder(viewHolder, position);
            }

            return viewHolder;
        }

        private bool TryBindViewHolder(RecyclerViewHolder viewHolder, int position)
        {
            if (!_recyclerView.Adapter)
            {
                return false;
            }

            viewHolder.RecyclerView = _recyclerView;
            _recyclerView.Adapter.BindViewHolder(viewHolder, position);
            return true;
        }

        private void RecycleViewHolderInternal(RecyclerViewHolder viewHolder)
        {
            var cached = false;
            var recycled = false;
            if (_maxCacheSize > 0 &&
                !viewHolder.HasFlags(RecyclerViewHolder.FlagRemoved | RecyclerViewHolder.FlagUpdate))
            {
                if (_cachedViewHolders.Count >= _maxCacheSize)
                {
                    RecycleCachedViewHolderAt(0);
                }

                _cachedViewHolders.Add(viewHolder);
                cached = true;
            }

            if (!cached && _recycledViewPool.IsViewHolderRecyclable(viewHolder))
            {
                AddViewHolderToRecycledViewPool(viewHolder);
                recycled = true;
            }

            if (!cached && !recycled)
            {
                _recyclerView.Adapter?.DestroyViewHolder(viewHolder);
            }
        }

        private void RecycleCachedViewHolderAt(int index)
        {
            var viewHolder = _cachedViewHolders[index];
            AddViewHolderToRecycledViewPool(viewHolder);
            _cachedViewHolders.RemoveAt(index);
        }

        private void AddViewHolderToRecycledViewPool(RecyclerViewHolder viewHolder)
        {
            _recyclerView.Adapter?.RecycleViewHolder(viewHolder);
            viewHolder.RecyclerView = null;
            _recycledViewPool.PutRecycledViewHolder(viewHolder);
        }
    }
}