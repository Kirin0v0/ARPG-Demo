using System.Collections.Generic;
using System.Linq;
using Framework.Common.Debug;
using Framework.Common.UI.RecyclerView.Cache;
using Framework.Common.UI.RecyclerView.Scroller;
using UnityEngine;

namespace Framework.Common.UI.RecyclerView.LayoutManager
{
    public class RecyclerViewLinearLayoutManager : RecyclerViewLayoutManager
    {
        private abstract class LayoutCacheData
        {
        }

        private class LayoutEmptyCacheData : LayoutCacheData
        {
        }

        private class LayoutInScreenCacheData : LayoutCacheData
        {
            public int Start;
            public int End;
        }

        private class LayoutOutScreenCacheData : LayoutCacheData
        {
            public enum OutScreenPosition
            {
                Top,
                Bottom
            }

            public OutScreenPosition Position;
        }

        public enum Orientation
        {
            Horizontal,
            Vertical,
        }

        public enum FocusStrategy
        {
            MinScrollDistance,
            ShowAsFirstItem,
        }

        [Header("布局相关")] [SerializeField] private Orientation orientation;
        public Orientation GetOrientation() => orientation;
        [SerializeField] private RectOffset padding;
        public RectOffset GetPadding() => padding;
        [SerializeField] private float spacing;
        public float GetSpacing() => spacing;

        [Header("滚动相关")] [SerializeField] private bool scroll = true;
        [SerializeField] private bool snap = false;
        [Min(1f)] [SerializeField] private float dragSpeed = 1f;
        [Min(1f)] [SerializeField] private float wheelSpeed = 10f;
        [Min(0f)] [SerializeField] private float minSmoothScrollTime = 0.05f;
        [Min(0f)] [SerializeField] private float maxSmoothScrollTime = 0.5f;

        [Header("滚动条相关")] [SerializeField] private bool showScrollbar = false;
        [SerializeField] private RecyclerViewScrollbar scrollbar;

        [Header("聚焦相关")] [SerializeField] private FocusStrategy focusStrategy;

        private RecyclerViewCommonScroller _scroller;
        private float _scrollPosition;
        private float _scrollDelta;
        public override bool Scrolling => _scroller.Scrolling;

        // 测量ViewHolder子项位置
        private readonly Dictionary<int, (float start, float end, Vector2 size)> _measuredPositions = new();

        // 布局缓存数据
        private LayoutCacheData _layoutCacheData;

        protected override void OnAttachToRecyclerView(RecyclerView recyclerView)
        {
            if (scroll)
            {
                var olderScrollers = recyclerView.Content.GetComponents<RecyclerViewScroller>();
                foreach (var scroller in olderScrollers)
                {
                    Destroy(scroller);
                }
                _scroller = recyclerView.Content.gameObject.AddComponent<RecyclerViewCommonScroller>();
                _scroller.ScrollOrientation = orientation switch
                {
                    Orientation.Horizontal => RecyclerViewCommonScroller.Orientation.Horizontal,
                    Orientation.Vertical => RecyclerViewCommonScroller.Orientation.Vertical,
                };
                _scroller.ViewportSize = orientation switch
                {
                    Orientation.Horizontal => ViewportSize.x,
                    Orientation.Vertical => ViewportSize.y,
                };
                _scroller.Snap = snap;
                _scroller.DragSpeed = dragSpeed;
                _scroller.WheelSpeed = wheelSpeed;
                _scroller.PositionChangedEvent += OnScrollPositionChanged;

                if (scrollbar)
                {
                    scrollbar.gameObject.SetActive(showScrollbar);
                    scrollbar.Scrollbar.onValueChanged.AddListener(OnScrollbarValueChanged);
                }
            }
            else
            {
                if (scrollbar)
                {
                    scrollbar.gameObject.SetActive(false);
                }
            }

            _scrollPosition = 0f;
            _scrollDelta = 0f;
            _measuredPositions.Clear();
            _layoutCacheData = null;
        }

        protected override void OnMeasure(Recycler recycler)
        {
            // 每次测量都清空数据
            _measuredPositions.Clear();
            _layoutCacheData = null;

            var adapter = RecyclerView.Adapter;
            if (!adapter)
            {
                return;
            }

            // 计算内容尺寸大小
            ContentSize = orientation switch
            {
                Orientation.Horizontal => new Vector2(0, ContentSize.y),
                Orientation.Vertical => new Vector2(ContentSize.x, 0),
                _ => ContentSize
            };
            var position = 0f;
            var itemCount = adapter.GetItemCount();
            for (var i = 0; i < itemCount; i++)
            {
                var viewType = adapter.GetItemViewType(i);
                var viewHolderTemplate = adapter.GetViewHolderTemplate(viewType);
                var size = adapter.MeasureViewHolderTemplate(viewHolderTemplate, i,
                    new Vector2(
                        Mathf.Max(0, ViewportSize.x - padding.left - padding.right),
                        Mathf.Max(0, ViewportSize.y - padding.top - padding.bottom)
                    )
                );
                // 计算每个子项的起始和结束位置
                switch (orientation)
                {
                    case Orientation.Horizontal:
                    {
                        // 子项前置计算
                        if (i == 0)
                        {
                            ContentSize += new Vector2(padding.left, 0);
                            position += padding.left;
                        }
                        else
                        {
                            ContentSize += new Vector2(spacing, 0);
                            position += spacing;
                        }

                        // 子项自身计算
                        var start = position;
                        ContentSize += new Vector2(size.x, 0);
                        position += size.x;
                        var end = position;
                        _measuredPositions.Add(i, (start, end, size));

                        // 子项后置计算
                        if (i == itemCount - 1)
                        {
                            ContentSize += new Vector2(padding.right, 0);
                            position += padding.right;
                        }
                    }
                        break;
                    case Orientation.Vertical:
                    {
                        // 子项前置计算
                        if (i == 0)
                        {
                            ContentSize += new Vector2(0, padding.top);
                            position += padding.top;
                        }
                        else
                        {
                            ContentSize += new Vector2(0, spacing);
                            position += spacing;
                        }

                        // 子项自身计算
                        var start = position;
                        ContentSize += new Vector2(0, size.y);
                        position += size.y;
                        var end = position;
                        _measuredPositions.Add(i, (start, end, size));

                        // 子项后置计算
                        if (i == itemCount - 1)
                        {
                            ContentSize += new Vector2(0, padding.bottom);
                            position += padding.bottom;
                        }
                    }
                        break;
                }
            }

            // 如果滚动，就设置滚动器的最小和最大滚动位置
            if (!scroll) return;
            _scroller.MinPosition = 0f;
            _scroller.MaxPosition = orientation switch
            {
                Orientation.Horizontal => Mathf.Max(0f, ContentSize.x - ViewportSize.x),
                Orientation.Vertical => Mathf.Max(0f, ContentSize.y - ViewportSize.y),
            };
            // 如果存在滚动条，就设置滚动条尺寸
            if (scrollbar)
            {
                scrollbar.Scrollbar.size = orientation switch
                {
                    Orientation.Horizontal => ViewportSize.x / ContentSize.x,
                    Orientation.Vertical => ViewportSize.y / ContentSize.y,
                };
            }
        }

        protected override void OnLayoutChildren(Recycler recycler)
        {
            var adapter = RecyclerView.Adapter;
            if (!adapter)
            {
                return;
            }

            RecycleAndCalculateViewHolders(recycler);
            LayoutViewHolders(recycler);
        }

        protected override void OnDetachFromRecyclerView(RecyclerView recyclerView)
        {
            if (scroll)
            {
                Destroy(_scroller);
                _scroller = null;

                if (scrollbar)
                {
                    scrollbar.gameObject.SetActive(showScrollbar);
                    scrollbar.Scrollbar.onValueChanged.RemoveListener(OnScrollbarValueChanged);
                }
            }
        }

        public override void ScrollToPosition(float position)
        {
            if (!_scroller)
            {
                return;
            }

            _scroller.ScrollToPosition(position);
        }

        public override void SmoothScrollToPosition(float position)
        {
            if (!_scroller)
            {
                return;
            }

            var scrollDistance = Mathf.Abs(_scrollPosition - position);
            var scrollDistanceRatio = Mathf.Approximately(_scroller.MinPosition, _scroller.MaxPosition)
                ? 1f
                : scrollDistance / (_scroller.MaxPosition - _scroller.MinPosition);
            var scrollTime = scrollDistanceRatio * (maxSmoothScrollTime - minSmoothScrollTime) + minSmoothScrollTime;
            _scroller.SmoothScrollToPosition(position, scrollDistance / scrollTime);
        }

        public override void StopSmoothScroll()
        {
            if (!_scroller)
            {
                return;
            }

            _scroller.StopSmoothScroll();
        }

        public override void ScrollDelta(float delta)
        {
            ScrollToPosition(_scrollPosition + delta);
        }

        public override void FocusItem(int index, bool smoothScroll)
        {
            if (!_measuredPositions.TryGetValue(index, out var layout))
            {
                return;
            }

            switch (focusStrategy)
            {
                case FocusStrategy.MinScrollDistance:
                {
                    if (!IsViewHolderFullVisible(layout.start, layout.end))
                    {
                        float target;
                        if (_scrollPosition >= layout.start)
                        {
                            target = layout.start;
                        }
                        else
                        {
                            target = _scrollPosition + layout.end - (_scrollPosition + orientation switch
                            {
                                Orientation.Horizontal => ViewportSize.x,
                                Orientation.Vertical => ViewportSize.y,
                            });
                        }

                        if (smoothScroll)
                        {
                            SmoothScrollToPosition(target);
                        }
                        else
                        {
                            ScrollToPosition(target);
                        }
                    }
                }
                    break;
                case FocusStrategy.ShowAsFirstItem:
                {
                    if (smoothScroll)
                    {
                        SmoothScrollToPosition(layout.start);
                    }
                    else
                    {
                        ScrollToPosition(layout.start);
                    }
                }
                    break;
            }
        }

        public override bool IsViewHolderVisible(int position)
        {
            if (_measuredPositions.TryGetValue(position, out var measuredPosition))
            {
                return IsViewHolderVisible(measuredPosition.start, measuredPosition.end);
            }
            else
            {
                return false;
            }
        }

        private void OnScrollPositionChanged(float position)
        {
            _scrollDelta = position - _scrollPosition;
            _scrollPosition = position;
            RecyclerView.RequestLayout();
            if (Mathf.Approximately(_scroller.MinPosition, _scroller.MaxPosition))
            {
                scrollbar?.Scrollbar?.SetValueWithoutNotify(0f);
            }
            else
            {
                scrollbar?.Scrollbar?.SetValueWithoutNotify(
                    Mathf.Clamp01((position - _scroller.MinPosition) / (_scroller.MaxPosition - _scroller.MinPosition))
                );
            }

            OnScrolled?.Invoke();
        }

        private void OnScrollbarValueChanged(float value)
        {
            ScrollToPosition(value * (_scroller.MaxPosition - _scroller.MinPosition) + _scroller.MinPosition);
        }

        private void RecycleAndCalculateViewHolders(Recycler recycler)
        {
            // 判断缓存数据是否存在或为空数据
            switch (_layoutCacheData)
            {
                // 如果不存在缓存数据，代表本次是第一次计算缓存数据，清除可见子项，并计算当前可见子项作为缓存数据
                case null:
                {
                    var viewHolders = recycler.GetVisibleViewHolders();
                    foreach (var viewHolder in viewHolders)
                    {
                        recycler.HideViewHolder(viewHolder);
                        recycler.RecycleViewHolder(viewHolder);
                    }

                    // 如果测量子项数量为空，则认为不存在数据，就没必要布局了
                    if (_measuredPositions.Count == 0)
                    {
                        _layoutCacheData = new LayoutEmptyCacheData();
                        return;
                    }

                    // 先判断极限情况，即可见子项全在屏幕上方或下方，在屏幕内才遍历取出可见子项
                    var cacheOutScreenData = CalculateOutScreen();
                    if (cacheOutScreenData != null)
                    {
                        _layoutCacheData = cacheOutScreenData;
                    }
                    else
                    {
                        // 这里不采用二分法的原因是从业务场景来看，一般是初始化才会没有缓存数据，此时一般没有滚动，即从头开始最快
                        var findVisible = false;
                        var firstVisibleIndex = RecyclerView.NoPosition;
                        var lastVisibleIndex = RecyclerView.NoPosition;
                        for (var i = 0; i < _measuredPositions.Count; i++)
                        {
                            var measuredPosition = _measuredPositions[i];
                            if (IsViewHolderVisible(measuredPosition.start, measuredPosition.end))
                            {
                                if (findVisible)
                                {
                                    lastVisibleIndex = i;
                                }
                                else
                                {
                                    firstVisibleIndex = i;
                                    lastVisibleIndex = i;
                                }

                                findVisible = true;
                            }
                            else if (findVisible)
                            {
                                break;
                            }
                        }

                        _layoutCacheData = new LayoutInScreenCacheData
                        {
                            Start = firstVisibleIndex,
                            End = lastVisibleIndex,
                        };
                    }

                    return;
                }
                // 判断缓存数据是否是空数据
                case LayoutEmptyCacheData emptyData:
                    // 空数据直接返回，不需要回收
                    return;
            }

            // 判断当前可见子项的极限情况，即可见子项全在屏幕上方或下方
            var newestOutScreenData = CalculateOutScreen();
            if (newestOutScreenData != null)
            {
                // 判断先前缓存数据类型
                switch (_layoutCacheData)
                {
                    case LayoutInScreenCacheData inScreenData:
                    {
                        // 回收子项
                        for (var i = inScreenData.Start; i <= inScreenData.End; i++)
                        {
                            var recycleViewHolder = recycler.GetVisibleViewHolderForPosition(i);
                            if (!recycleViewHolder) continue;
                            recycler.HideViewHolder(recycleViewHolder);
                            recycler.RecycleViewHolder(recycleViewHolder);
                        }
                    }
                        break;
                }

                _layoutCacheData = newestOutScreenData;
                return;
            }

            // 到了这里则代表当前可见子项处于屏幕内，则根据先前缓存数据执行不同逻辑
            switch (_layoutCacheData)
            {
                case LayoutOutScreenCacheData outScreenData:
                {
                    // 如果先前缓存数据处于屏幕外，则根据进入屏幕方向计算可见子项索引范围
                    var firstVisibleIndex = RecyclerView.NoPosition;
                    var lastVisibleIndex = RecyclerView.NoPosition;
                    switch (outScreenData.Position)
                    {
                        case LayoutOutScreenCacheData.OutScreenPosition.Top:
                        {
                            // 如果从上方进入屏幕，就从起始子项开始
                            var findVisible = false;
                            for (var i = 0; i < _measuredPositions.Count; i++)
                            {
                                var measuredPosition = _measuredPositions[i];
                                if (IsViewHolderVisible(measuredPosition.start, measuredPosition.end))
                                {
                                    if (findVisible)
                                    {
                                        lastVisibleIndex = i;
                                    }
                                    else
                                    {
                                        firstVisibleIndex = i;
                                        lastVisibleIndex = i;
                                    }

                                    findVisible = true;
                                }
                                else if (findVisible)
                                {
                                    break;
                                }
                            }
                        }
                            break;
                        case LayoutOutScreenCacheData.OutScreenPosition.Bottom:
                        {
                            // 如果从下方进入屏幕，就从终点子项开始
                            var findVisible = false;
                            for (var i = _measuredPositions.Count - 1; i >= 0; i--)
                            {
                                var measuredPosition = _measuredPositions[i];
                                if (IsViewHolderVisible(measuredPosition.start, measuredPosition.end))
                                {
                                    if (findVisible)
                                    {
                                        firstVisibleIndex = i;
                                    }
                                    else
                                    {
                                        lastVisibleIndex = i;
                                        firstVisibleIndex = i;
                                    }

                                    findVisible = true;
                                }
                                else if (findVisible)
                                {
                                    break;
                                }
                            }
                        }
                            break;
                    }

                    var inScreenData = new LayoutInScreenCacheData
                    {
                        Start = firstVisibleIndex,
                        End = lastVisibleIndex,
                    };
                    _layoutCacheData = inScreenData;
                }
                    break;
                case LayoutInScreenCacheData inScreenData:
                {
                    // 根据滚动差值计算最新可见子项
                    LayoutInScreenCacheData newestInScreenData = null;
                    switch (_scrollDelta)
                    {
                        case 0:
                        {
                            newestInScreenData = inScreenData;
                        }
                            break;
                        case > 0:
                        {
                            // 从先前第一个可见子项开始找再次可见的子项索引
                            var startIndex = inScreenData.Start;
                            while (inScreenData.Start < _measuredPositions.Count)
                            {
                                if (!IsViewHolderVisible(_measuredPositions[startIndex].start,
                                        _measuredPositions[startIndex].end))
                                {
                                    startIndex++;
                                }
                                else
                                {
                                    break;
                                }
                            }

                            // 从先前最后一个可见子项开始找不可见的子项索引
                            var endIndex = Mathf.Max(startIndex, inScreenData.End);
                            while (endIndex + 1 < _measuredPositions.Count)
                            {
                                if (IsViewHolderVisible(_measuredPositions[endIndex + 1].start,
                                        _measuredPositions[endIndex + 1].end))
                                {
                                    endIndex++;
                                }
                                else
                                {
                                    break;
                                }
                            }

                            newestInScreenData = new LayoutInScreenCacheData
                            {
                                Start = startIndex,
                                End = endIndex,
                            };

                            break;
                        }
                        case < 0:
                        {
                            // 从先前最后一个可见子项开始找再次可见的子项索引
                            var endIndex = inScreenData.End;
                            while (endIndex >= 0)
                            {
                                if (!IsViewHolderVisible(_measuredPositions[endIndex].start,
                                        _measuredPositions[endIndex].end))
                                {
                                    endIndex--;
                                }
                                else
                                {
                                    break;
                                }
                            }

                            // 从先前第一个可见子项开始找不可见的子项索引
                            var startIndex = Mathf.Min(endIndex, inScreenData.Start);
                            while (startIndex - 1 >= 0)
                            {
                                if (IsViewHolderVisible(_measuredPositions[startIndex - 1].start,
                                        _measuredPositions[startIndex - 1].end))
                                {
                                    startIndex--;
                                }
                                else
                                {
                                    break;
                                }
                            }

                            newestInScreenData = new LayoutInScreenCacheData
                            {
                                Start = startIndex,
                                End = endIndex,
                            };

                            break;
                        }
                    }

                    // 回收转为不可见的子项
                    var newestSet = new HashSet<int>(Enumerable.Range(newestInScreenData.Start,
                        newestInScreenData.End - newestInScreenData.Start + 1));
                    var originSet = new HashSet<int>(Enumerable.Range(inScreenData.Start,
                        inScreenData.End - inScreenData.Start + 1));
                    var intersection = new HashSet<int>(newestSet.Intersect(originSet));
                    originSet.ExceptWith(intersection);
                    var recycledViewHolderIndexes = originSet.ToList();
                    recycledViewHolderIndexes.ForEach(index =>
                    {
                        var recycleViewHolder = recycler.GetVisibleViewHolderForPosition(index);
                        if (!recycleViewHolder) return;
                        recycler.HideViewHolder(recycleViewHolder);
                        recycler.RecycleViewHolder(recycleViewHolder);
                    });

                    _layoutCacheData = newestInScreenData;
                }
                    break;
            }

            return;

            LayoutOutScreenCacheData CalculateOutScreen()
            {
                var start = 0;
                var end = _measuredPositions.Count - 1;
                var endViewHolderAboveScreen = IsViewHolderAboveScreen(_measuredPositions[end].end);
                var startViewHolderBelowScreen = IsViewHolderBelowScreen(_measuredPositions[start].start);
                if (endViewHolderAboveScreen.above)
                {
                    return new LayoutOutScreenCacheData
                    {
                        Position = LayoutOutScreenCacheData.OutScreenPosition.Top,
                    };
                }

                if (startViewHolderBelowScreen.below)
                {
                    return new LayoutOutScreenCacheData
                    {
                        Position = LayoutOutScreenCacheData.OutScreenPosition.Bottom,
                    };
                }

                return null;
            }
        }

        private void LayoutViewHolders(Recycler recycler)
        {
            var start = RecyclerView.NoPosition;
            var end = RecyclerView.NoPosition;
            switch (_layoutCacheData)
            {
                case LayoutInScreenCacheData inScreenCacheData:
                {
                    start = inScreenCacheData.Start;
                    end = inScreenCacheData.End;
                }
                    break;
                default:
                    return;
            }

            var contentOffset = orientation switch
            {
                Orientation.Horizontal => ViewportSize.x / 2 + _scrollPosition,
                Orientation.Vertical => -(ViewportSize.y / 2 + _scrollPosition),
            };
            for (var i = start; i <= end; i++)
            {
                var viewHolder = recycler.GetViewHolderForPosition(i);
                if (!viewHolder)
                {
                    continue;
                }

                // 设置子项位置和尺寸
                var layout = _measuredPositions[i];
                viewHolder.RectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                viewHolder.RectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                viewHolder.RectTransform.pivot = new Vector2(0.5f, 0.5f);
                viewHolder.RectTransform.anchoredPosition = orientation switch
                {
                    Orientation.Horizontal => new Vector2(
                        (layout.start - contentOffset + layout.end - contentOffset) / 2f,
                        (padding.bottom - padding.top) / 2f
                    ),
                    Orientation.Vertical => new Vector2(
                        (padding.left - padding.right) / 2f,
                        (-layout.start - contentOffset + -layout.end - contentOffset) / 2f
                    ),
                };
                viewHolder.RectTransform.sizeDelta = layout.size;

                // 展示子项
                recycler.ShowViewHolder(viewHolder);
            }
        }

        private bool IsViewHolderFullVisible(float start, float end)
        {
            var viewportSize = orientation switch
            {
                Orientation.Horizontal => ViewportSize.x,
                Orientation.Vertical => ViewportSize.y,
            };
            return !(start < _scrollPosition || end > _scrollPosition + viewportSize);
        }

        private bool IsViewHolderVisible(float start, float end)
        {
            var viewportSize = orientation switch
            {
                Orientation.Horizontal => ViewportSize.x,
                Orientation.Vertical => ViewportSize.y,
            };
            return (start >= _scrollPosition && start < _scrollPosition + viewportSize) ||
                   (end > _scrollPosition && end <= _scrollPosition + viewportSize);
        }

        private (bool above, float distance) IsViewHolderAboveScreen(float end)
        {
            return (end <= _scrollPosition, end - _scrollPosition);
        }

        private (bool below, float distance) IsViewHolderBelowScreen(float start)
        {
            var viewportSize = orientation switch
            {
                Orientation.Horizontal => ViewportSize.x,
                Orientation.Vertical => ViewportSize.y,
            };
            return (start >= _scrollPosition + viewportSize, start - _scrollPosition - viewportSize);
        }
    }
}