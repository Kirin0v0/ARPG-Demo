using System.Collections;
using System.Collections.Generic;
using Framework.Common.Debug;
using Framework.Common.UI.RecyclerView.Cache;
using Framework.Common.Util;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Framework.Common.UI.RecyclerView.Adapter
{
    public abstract class RecyclerViewAdapter : MonoBehaviour
    {
        public RecyclerView RecyclerView { set; private get; }

        private readonly List<RecyclerViewAdapterDataObserver> _observers = new();

        protected abstract IList GetData();

        public abstract int GetItemViewType(int position);

        public int GetItemCount() => GetData().Count;

        public void SetData(IList data)
        {
            var list = GetData();
            list.Clear();
            foreach (var item in data)
            {
                list.Add(item);
            }

            NotifyDataSetChanged();
        }

        public void RefreshItem(int position, object item)
        {
            var list = GetData();
            list[position] = item;
            NotifyItemChanged(position);
        }

        [CanBeNull]
        public abstract RecyclerViewHolder GetViewHolderTemplate(int viewType);

        /// <summary>
        /// 传入模板ViewHolder和ViewHolder索引，推算出指定ViewHolder的尺寸
        /// </summary>
        /// <param name="viewHolder">ViewHolder模板</param>
        /// <param name="position">数据索引</param>
        /// <param name="constraintSize">父布局约束尺寸，用于重计算ViewHolder尺寸</param>
        /// <returns></returns>
        public virtual Vector2 MeasureViewHolderTemplate([CanBeNull] RecyclerViewHolder viewHolder, int position,
            Vector2 constraintSize)
        {
            if (!viewHolder)
            {
                return Vector2.zero;
            }

            // 记录ViewHolder原始尺寸
            var originSize = viewHolder.RectTransform.rect.size;

            // 根据约束条件重新设置ViewHolder尺寸
            var size = new Vector2(Mathf.Min(originSize.x, constraintSize.x),
                Mathf.Min(originSize.y, constraintSize.y));
            if (UGUIUtil.IsWidthStretched(viewHolder.RectTransform))
            {
                size.x = constraintSize.x + viewHolder.RectTransform.sizeDelta.x;
            }

            if (UGUIUtil.IsHeightStretched(viewHolder.RectTransform))
            {
                size.y = constraintSize.y + viewHolder.RectTransform.sizeDelta.y;
            }

            // 这里根据约束尺寸重新设置ViewHolder尺寸，仅用于测量使用
            viewHolder.RectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size.x);
            viewHolder.RectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size.y);

            // 由于GameObject的激活/失活会占用性能，这里需要谨慎判断是否需要激活对象
            var needToEnable = viewHolder.gameObject.TryGetComponent<ContentSizeSynchronizer>(out _) ||
                               viewHolder.gameObject.TryGetComponent<ContentSizeFitter>(out _);
            if (needToEnable)
            {
                // 保证ViewHolder激活
                var activeSelf = viewHolder.gameObject.activeSelf;
                var parent = viewHolder.gameObject.transform.parent;
                if (!viewHolder.gameObject.activeInHierarchy)
                {
                    viewHolder.gameObject.transform.SetParent(null);
                }

                if (!activeSelf)
                {
                    viewHolder.gameObject.SetActive(true);
                }

                // 根据ViewHolder绑定组件执行不同计算逻辑
                if (viewHolder.TryGetComponent<ContentSizeSynchronizer>(out var contentSizeSynchronizer))
                {
                    contentSizeSynchronizer.CalculateSize();
                    size = viewHolder.RectTransform.rect.size;
                }

                if (viewHolder.TryGetComponent<ContentSizeFitter>(out var contentSizeFitter))
                {
                    UGUIUtil.RefreshLayoutGroupsImmediateAndRecursive(viewHolder.gameObject);
                    size = viewHolder.RectTransform.rect.size;
                }

                // 恢复ViewHolder状态
                viewHolder.gameObject.transform.SetParent(parent);
                viewHolder.gameObject.SetActive(activeSelf);
            }
            else
            {
                size = viewHolder.RectTransform.rect.size;
            }

            // 恢复原始尺寸
            viewHolder.RectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, originSize.x);
            viewHolder.RectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, originSize.y);

            return size;
        }

        /// <summary>
        /// Recycler关联Adapter时调用
        /// </summary>
        /// <param name="recyclerView"></param>
        public void AttachToRecyclerView(RecyclerView recyclerView)
        {
            OnAttachToRecyclerView(recyclerView);
        }

        /// <summary>
        /// Recycler创建ViewHolder时调用
        /// </summary>
        /// <param name="viewType"></param>
        /// <returns></returns>
        public RecyclerViewHolder CreateViewHolder(int viewType)
        {
            var viewHolder = OnCreateViewHolder(viewType, GetViewHolderTemplate(viewType));
            if (!viewHolder)
            {
                return null;
            }

            viewHolder.ViewType = viewType;
            return viewHolder;
        }

        /// <summary>
        /// Recycler在ViewHolder绑定数据时调用
        /// 注意，这里ViewHolder的尺寸可能存在异常，如果一定需要用到正确尺寸（例如ViewHolder是RecyclerView需要调用SetData），则请在ShowViewHolder中调用
        /// </summary>
        /// <param name="viewHolder"></param>
        /// <param name="position"></param>
        public void BindViewHolder(RecyclerViewHolder viewHolder, int position)
        {
            viewHolder.Position = position;
            viewHolder.SetFlags(RecyclerViewHolder.FlagBound,
                RecyclerViewHolder.FlagBound | RecyclerViewHolder.FlagUpdate | RecyclerViewHolder.FlagRemoved);
            OnBindViewHolder(viewHolder, position);
        }

        /// <summary>
        /// LayoutManager在设置正确尺寸和位置并展示ViewHolder时调用
        /// </summary>
        /// <param name="viewHolder"></param>
        public void ShowViewHolder(RecyclerViewHolder viewHolder)
        {
            OnShowViewHolder(viewHolder);
        }

        /// <summary>
        /// LayoutManager在隐藏ViewHolder时调用
        /// </summary>
        /// <param name="viewHolder"></param>
        public void HideViewHolder(RecyclerViewHolder viewHolder)
        {
            OnHideViewHolder(viewHolder);
        }

        /// <summary>
        /// Recycler回收ViewHolder的数据时调用
        /// </summary>
        /// <param name="viewHolder"></param>
        public void RecycleViewHolder(RecyclerViewHolder viewHolder)
        {
            OnRecycleViewHolder(viewHolder);
            viewHolder.SetFlags(RecyclerViewHolder.FlagRemoved,
                RecyclerViewHolder.FlagBound | RecyclerViewHolder.FlagUpdate | RecyclerViewHolder.FlagRemoved);
        }

        /// <summary>
        /// Recycler在回收池溢满销毁ViewHolder时调用
        /// </summary>
        /// <param name="viewHolder"></param>
        public void DestroyViewHolder(RecyclerViewHolder viewHolder)
        {
            OnDestroyViewHolder(viewHolder);
            viewHolder.Reset();
            GameObject.Destroy(viewHolder.gameObject);
        }

        /// <summary>
        /// Recycler解除关联Adapter时调用
        /// </summary>
        /// <param name="recyclerView"></param>
        public void DetachFromRecyclerView(RecyclerView recyclerView)
        {
            OnDetachFromRecyclerView(recyclerView);
        }

        public void RegisterAdapterDataObserver(RecyclerViewAdapterDataObserver observer)
        {
            lock (_observers)
            {
                if (!_observers.Contains(observer))
                {
                    _observers.Add(observer);
                }
            }
        }

        public void UnregisterAdapterDataObserver(RecyclerViewAdapterDataObserver observer)
        {
            lock (_observers)
            {
                _observers.Remove(observer);
            }
        }

        public void NotifyDataSetChanged()
        {
            lock (_observers)
            {
                _observers.ForEach(observer => observer.OnChanged());
            }
        }

        public void NotifyItemChanged(int position, object payload = null)
        {
            lock (_observers)
            {
                _observers.ForEach(observer => observer.OnItemRangeChanged(position, 1, payload));
            }
        }

        public void NotifyItemRangeChanged(int positionStart, int itemCount, object payload = null)
        {
            lock (_observers)
            {
                _observers.ForEach(observer => observer.OnItemRangeChanged(positionStart, itemCount, payload));
            }
        }

        // public void NotifyItemInserted(int position)
        // {
        //     lock (_observers)
        //     {
        //         _observers.ForEach(observer => observer.OnItemRangeInserted(position, 1));
        //     }
        // }
        //
        // public void NotifyItemRangeInserted(int positionStart, int itemCount)
        // {
        //     lock (_observers)
        //     {
        //         _observers.ForEach(observer => observer.OnItemRangeInserted(positionStart, itemCount));
        //     }
        // }
        //
        // public void NotifyItemMoved(int fromPosition, int toPosition)
        // {
        //     lock (_observers)
        //     {
        //         _observers.ForEach(observer => observer.OnItemRangeMoved(fromPosition, toPosition, 1));
        //     }
        // }
        //
        // public void NotifyItemRemoved(int position)
        // {
        //     lock (_observers)
        //     {
        //         _observers.ForEach(observer => observer.OnItemRangeRemoved(position, 1));
        //     }
        // }
        //
        // public void NotifyItemRangeRemoved(int positionStart, int itemCount)
        // {
        //     lock (_observers)
        //     {
        //         _observers.ForEach(observer => observer.OnItemRangeRemoved(positionStart, itemCount));
        //     }
        // }

        protected virtual void OnAttachToRecyclerView(RecyclerView recyclerView)
        {
        }

        protected abstract RecyclerViewHolder OnCreateViewHolder(int viewType,
            [CanBeNull] RecyclerViewHolder viewHolderTemplate);

        protected abstract void OnBindViewHolder(RecyclerViewHolder viewHolder, int position);

        protected virtual void OnShowViewHolder(RecyclerViewHolder viewHolder)
        {
        }

        protected virtual void OnHideViewHolder(RecyclerViewHolder viewHolder)
        {
        }

        protected abstract void OnRecycleViewHolder(RecyclerViewHolder viewHolder);

        protected abstract void OnDestroyViewHolder(RecyclerViewHolder viewHolder);

        protected virtual void OnDetachFromRecyclerView(RecyclerView recyclerView)
        {
        }
    }
}