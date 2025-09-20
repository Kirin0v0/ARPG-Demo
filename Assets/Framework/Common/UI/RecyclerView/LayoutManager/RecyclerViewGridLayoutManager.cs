using System.Collections.Generic;
using System.Linq;
using Framework.Common.UI.RecyclerView.Cache;
using Framework.Common.UI.RecyclerView.Scroller;
using UnityEngine;

namespace Framework.Common.UI.RecyclerView.LayoutManager
{
    public class RecyclerViewGridLayoutManager : RecyclerViewLayoutManager
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
        [Min(1)] [SerializeField] private int spanCount = 1;
        public int GetSpanCount() => spanCount;
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

        private ISpanSizeLookup _spanSizeLookup = new DefaultSpanSizeLookup();

        // 测量数据
        private readonly Dictionary<int, List<int>> _measuredStretchSpanViewHolders = new(); // 每个网格行/列的子项列表

        private readonly Dictionary<int, (float start, float end)>
            _measuredStretchSpanPosition = new(); // 每个网格行/列的起始和结束位置

        private readonly Dictionary<int, (int stretchIndex, Vector2 position, Vector2 size)>
            _measuredViewHolderPositions = new(); // 每个子项对应的网格行/列索引以及布局位置

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
                    _ => RecyclerViewCommonScroller.Orientation.Vertical
                };
                _scroller.ViewportSize = orientation switch
                {
                    Orientation.Horizontal => ViewportSize.x,
                    Orientation.Vertical => ViewportSize.y,
                    _ => ViewportSize.y
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
            _measuredStretchSpanViewHolders.Clear();
            _measuredStretchSpanPosition.Clear();
            _measuredViewHolderPositions.Clear();
            _layoutCacheData = null;
        }

        protected override void OnMeasure(Recycler recycler)
        {
            // 每次测量都清空数据
            _measuredStretchSpanViewHolders.Clear();
            _measuredStretchSpanPosition.Clear();
            _measuredViewHolderPositions.Clear();
            _layoutCacheData = null;

            var adapter = RecyclerView.Adapter;
            if (!adapter)
            {
                return;
            }

            var contentLength = MeasureGridLayout();
            ContentSize = orientation switch
            {
                Orientation.Horizontal => new Vector2(contentLength, ContentSize.y),
                Orientation.Vertical => new Vector2(ContentSize.x, contentLength),
                _ => ContentSize
            };

            if (scroll)
            {
                _scroller.MinPosition = 0f;
                _scroller.MaxPosition = orientation switch
                {
                    Orientation.Horizontal => Mathf.Max(0f, ContentSize.x - ViewportSize.x),
                    Orientation.Vertical => Mathf.Max(0f, ContentSize.y - ViewportSize.y),
                    _ => 0f
                };
                if (scrollbar)
                {
                    scrollbar.Scrollbar.size = orientation switch
                    {
                        Orientation.Horizontal => ViewportSize.x / ContentSize.x,
                        Orientation.Vertical => ViewportSize.y / ContentSize.y,
                        _ => 0f
                    };
                }
            }

            return;

            // 测量网格布局的长度（这里长度是指不固定尺寸的排列方向）
            float MeasureGridLayout()
            {
                var itemCount = adapter.GetItemCount();
                var spanUnitSize = orientation switch
                {
                    Orientation.Horizontal =>
                        (ViewportSize.y - padding.top - padding.bottom - spacing * (spanCount - 1)) / spanCount,
                    Orientation.Vertical =>
                        (ViewportSize.x - padding.left - padding.right - spacing * (spanCount - 1)) / spanCount,
                    _ => 0f
                };
                var spanSpacing = orientation switch
                {
                    Orientation.Horizontal => spacing,
                    Orientation.Vertical => spacing,
                    _ => 0f
                };
                var stretchSpacing = orientation switch
                {
                    Orientation.Horizontal => spacing,
                    Orientation.Vertical => spacing,
                    _ => 0f
                };

                var length = 0f; // 拓展延长方向的长度
                var stretchIndex = 0; // 拓展延长方向的索引
                var spanIndex = 0; // 固定尺寸行列的索引
                var maxStretchSize = 0f; // 该固定尺寸行列在拓展延长方向上的最大尺寸
                var preMeasurePositions = new Dictionary<int, (float, Vector2)>(); // 固定尺寸行列的预测量位置
                var firstSpanRow = true;

                // 从拓展延长方向依次记录其下的固定尺寸行列的位置
                for (var i = 0; i < itemCount; i++)
                {
                    // 如果固定行列索引达到最大值，就记录当前行列并创建新的行列
                    if (spanIndex >= spanCount)
                    {
                        // 这里认为只要在循环中始终不为最后一个固定尺寸的行列
                        RecordStretchSpan(firstSpanRow, false);
                        firstSpanRow = false;
                    }

                    // 先计算当前索引对应的子项所占的单位数量，如果小等于0则认为不展示，直接跳过
                    var spanSize = _spanSizeLookup.GetSpanSize(i);
                    if (spanSize <= 0)
                    {
                        continue;
                    }

                    var viewType = adapter.GetItemViewType(i);
                    var viewHolderTemplate = adapter.GetViewHolderTemplate(viewType);
                    var spanLayoutStartPosition = orientation switch
                    {
                        Orientation.Horizontal => padding.top + spanIndex * spanUnitSize + spanIndex * spanSpacing,
                        Orientation.Vertical => padding.left + spanIndex * spanUnitSize + spanIndex * spanSpacing,
                        _ => spanIndex * spanUnitSize + spanIndex * spanSpacing,
                    };
                    var maxSpanItemSize = spanSize * spanUnitSize + (spanSize - 1) * spanSpacing;
                    var itemSize = adapter.MeasureViewHolderTemplate(viewHolderTemplate, i,
                        orientation switch
                        {
                            Orientation.Horizontal =>
                                new Vector2(
                                    ViewportSize.x - padding.left - padding.right,
                                    maxSpanItemSize
                                ),
                            Orientation.Vertical => new Vector2(
                                maxSpanItemSize,
                                ViewportSize.y - padding.top - padding.bottom
                            ),
                            _ => ViewportSize,
                        }
                    );
                    var itemStretchSize = orientation switch
                    {
                        Orientation.Horizontal => itemSize.x,
                        Orientation.Vertical => itemSize.y,
                        _ => 0f
                    };

                    if (_measuredStretchSpanViewHolders.TryGetValue(stretchIndex, out var list))
                    {
                        list.Add(i);
                    }
                    else
                    {
                        _measuredStretchSpanViewHolders.Add(stretchIndex, new List<int> { i });
                    }

                    switch (orientation)
                    {
                        case Orientation.Horizontal:
                        {
                            preMeasurePositions.Add(i, (spanLayoutStartPosition + maxSpanItemSize / 2, itemSize));
                        }
                            break;
                        case Orientation.Vertical:
                        {
                            preMeasurePositions.Add(i, (spanLayoutStartPosition + maxSpanItemSize / 2, itemSize));
                        }
                            break;
                    }

                    spanIndex += spanSize;
                    maxStretchSize = Mathf.Max(maxStretchSize, itemStretchSize);
                }

                // 这里处理最后一个固定尺寸的行列
                if (itemCount != 0)
                {
                    RecordStretchSpan(firstSpanRow, true);
                }

                return length;

                // 记录伸长行列并另起一个新行列
                void RecordStretchSpan(bool isFirstSpan, bool isLastSpan)
                {
                    if (isFirstSpan)
                    {
                        length += orientation switch
                        {
                            Orientation.Horizontal => padding.left,
                            Orientation.Vertical => padding.top,
                            _ => 0f
                        };
                    }

                    // 先记录子项的位置，这里所有子项都会根据行列子项中的最大尺寸居中
                    switch (orientation)
                    {
                        case Orientation.Horizontal:
                            foreach (var keyValuePair in preMeasurePositions)
                            {
                                _measuredViewHolderPositions.Add(keyValuePair.Key,
                                    (stretchIndex, new Vector2(length + maxStretchSize / 2, keyValuePair.Value.Item1),
                                        keyValuePair.Value.Item2));
                            }

                            break;
                        case Orientation.Vertical:
                            foreach (var keyValuePair in preMeasurePositions)
                            {
                                _measuredViewHolderPositions.Add(keyValuePair.Key,
                                    (stretchIndex, new Vector2(keyValuePair.Value.Item1, length + maxStretchSize / 2),
                                        keyValuePair.Value.Item2));
                            }

                            break;
                    }

                    preMeasurePositions.Clear();
                    var start = length;
                    length += maxStretchSize;
                    var end = length;
                    _measuredStretchSpanPosition.Add(stretchIndex, (start, end));
                    maxStretchSize = 0f;
                    spanIndex = 0;
                    stretchIndex++;
                    if (!isLastSpan)
                    {
                        length += stretchSpacing;
                    }
                    else
                    {
                        length += orientation switch
                        {
                            Orientation.Horizontal => padding.right,
                            Orientation.Vertical => padding.bottom,
                            _ => 0f
                        };
                    }
                }
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
            LayoutVisibleViewHolders(recycler);
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

        public void SetSpanSizeLookup(ISpanSizeLookup spanSizeLookupImpl)
        {
            _spanSizeLookup = spanSizeLookupImpl;
            RecyclerView?.Refresh();
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
            if (!_measuredViewHolderPositions.TryGetValue(index, out var layout) ||
                !_measuredStretchSpanPosition.TryGetValue(layout.stretchIndex, out var stretchLayout))
            {
                return;
            }

            switch (focusStrategy)
            {
                case FocusStrategy.MinScrollDistance:
                {
                    if (!IsStretchSpanFullVisible(stretchLayout.start, stretchLayout.end))
                    {
                        float target;
                        if (_scrollPosition >= stretchLayout.start)
                        {
                            target = stretchLayout.start;
                        }
                        else
                        {
                            target = _scrollPosition + stretchLayout.end - (_scrollPosition + orientation switch
                            {
                                Orientation.Horizontal => ViewportSize.x,
                                Orientation.Vertical => ViewportSize.y,
                                _ => 0f
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
                        SmoothScrollToPosition(stretchLayout.start);
                    }
                    else
                    {
                        ScrollToPosition(stretchLayout.start);
                    }
                }
                    break;
            }
        }

        public override bool IsViewHolderVisible(int position)
        {
            if (!_measuredViewHolderPositions.TryGetValue(position, out var layout) ||
                !_measuredStretchSpanPosition.TryGetValue(layout.stretchIndex, out var stretchLayout))
            {
                return false;
            }

            return IsStretchSpanVisible(stretchLayout.start, stretchLayout.end);
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
            switch (_layoutCacheData)
            {
                // 如果不存在缓存数据，清除可见子项，并计算当前可见子项
                case null:
                {
                    var viewHolders = recycler.GetVisibleViewHolders();
                    foreach (var viewHolder in viewHolders)
                    {
                        recycler.HideViewHolder(viewHolder);
                        recycler.RecycleViewHolder(viewHolder);
                    }

                    // 如果测量子项数量为空，则认为不存在数据，就没必要布局了
                    if (_measuredViewHolderPositions.Count == 0)
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
                        // 这里不采用二分法的原因是从业务场景来说，一般是初始化才会没有缓存数据，此时一般没有滚动，即从头开始最快
                        var findVisible = false;
                        var firstVisibleIndex = RecyclerView.NoPosition;
                        var lastVisibleIndex = RecyclerView.NoPosition;
                        // 网格布局可见性直接按行列计算，而不是线性布局那样按子项计算
                        for (var i = 0; i < _measuredStretchSpanPosition.Count; i++)
                        {
                            var measuredPosition = _measuredStretchSpanPosition[i];
                            if (IsStretchSpanVisible(measuredPosition.start, measuredPosition.end))
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
                    // 空数据直接返回
                    return;
            }

            // 先判断极限情况，即可见子项全在屏幕上方或下方
            var newestCacheOutScreenData = CalculateOutScreen();
            if (newestCacheOutScreenData != null)
            {
                // 如果先前缓存数据存在子项，就回收全部子项
                if (_layoutCacheData is LayoutInScreenCacheData cacheInScreenData)
                {
                    for (var i = cacheInScreenData.Start; i <= cacheInScreenData.End; i++)
                    {
                        var recycleViewHolder = recycler.GetVisibleViewHolderForPosition(i);
                        if (!recycleViewHolder) continue;
                        recycler.HideViewHolder(recycleViewHolder);
                        recycler.RecycleViewHolder(recycleViewHolder);
                    }
                }

                _layoutCacheData = newestCacheOutScreenData;
                return;
            }

            // 到了这里则代表当前可见子项处于屏幕内，则根据先前缓存数据执行不同逻辑
            // 判断缓存数据是否是屏幕外数据
            switch (_layoutCacheData)
            {
                case LayoutOutScreenCacheData outScreenData:
                {
                    switch (outScreenData.Position)
                    {
                        case LayoutOutScreenCacheData.OutScreenPosition.Top:
                        {
                            // 如果从上方进入屏幕，就从起始子项开始
                            var findVisible = false;
                            var firstVisibleIndex = RecyclerView.NoPosition;
                            var lastVisibleIndex = RecyclerView.NoPosition;
                            for (var i = 0; i < _measuredStretchSpanPosition.Count; i++)
                            {
                                var measuredPosition = _measuredStretchSpanPosition[i];
                                if (IsStretchSpanVisible(measuredPosition.start, measuredPosition.end))
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
                            break;
                        case LayoutOutScreenCacheData.OutScreenPosition.Bottom:
                        {
                            // 如果从下方进入屏幕，就从终点子项开始
                            var findVisible = false;
                            var lastVisibleIndex = RecyclerView.NoPosition;
                            var firstVisibleIndex = RecyclerView.NoPosition;
                            for (var i = _measuredStretchSpanPosition.Count - 1; i >= 0; i--)
                            {
                                var measuredPosition = _measuredStretchSpanPosition[i];
                                if (IsStretchSpanVisible(measuredPosition.start, measuredPosition.end))
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

                            _layoutCacheData = new LayoutInScreenCacheData
                            {
                                Start = firstVisibleIndex,
                                End = lastVisibleIndex,
                            };
                        }
                            break;
                    }
                }
                    break;
                case LayoutInScreenCacheData inScreenData:
                {
                    // 根据滚动差值计算最新可见子项
                    var newestCacheInScreenData = CalculateInScreen(inScreenData.Start, inScreenData.End);

                    // 回收转为不可见的子项
                    var newestSet = new HashSet<int>(Enumerable.Range(newestCacheInScreenData.Start,
                        newestCacheInScreenData.End - newestCacheInScreenData.Start + 1));
                    var originSet = new HashSet<int>(Enumerable.Range(inScreenData.Start,
                        inScreenData.End - inScreenData.Start + 1));
                    var intersection = new HashSet<int>(newestSet.Intersect(originSet));
                    originSet.ExceptWith(intersection);
                    var recycledStretchSpanIndexes = originSet.ToList();
                    recycledStretchSpanIndexes.ForEach(stretchIndex =>
                    {
                        if (!_measuredStretchSpanViewHolders.TryGetValue(stretchIndex, out var viewHolderIndexes))
                        {
                            return;
                        }

                        viewHolderIndexes.ForEach(index =>
                        {
                            var recycleViewHolder = recycler.GetVisibleViewHolderForPosition(index);
                            if (!recycleViewHolder) return;
                            recycler.HideViewHolder(recycleViewHolder);
                            recycler.RecycleViewHolder(recycleViewHolder);
                        });
                    });

                    _layoutCacheData = newestCacheInScreenData;
                }
                    break;
            }

            return;

            LayoutOutScreenCacheData CalculateOutScreen()
            {
                var start = 0;
                var end = _measuredStretchSpanPosition.Count - 1;
                var endStretchSpanAboveScreen = IsStretchSpanAboveScreen(_measuredStretchSpanPosition[end].end);
                var startStretchSpanBelowScreen = IsStretchSpanBelowScreen(_measuredStretchSpanPosition[start].start);
                if (endStretchSpanAboveScreen.above)
                {
                    return new LayoutOutScreenCacheData
                    {
                        Position = LayoutOutScreenCacheData.OutScreenPosition.Top,
                    };
                }

                if (startStretchSpanBelowScreen.below)
                {
                    return new LayoutOutScreenCacheData
                    {
                        Position = LayoutOutScreenCacheData.OutScreenPosition.Bottom,
                    };
                }

                return null;
            }

            LayoutInScreenCacheData CalculateInScreen(int startIndex, int endIndex)
            {
                switch (_scrollDelta)
                {
                    case 0:
                        return new LayoutInScreenCacheData
                        {
                            Start = startIndex,
                            End = endIndex,
                        };
                    case > 0:
                    {
                        // 从先前第一个可见行列开始找再次可见的行列索引
                        while (startIndex < _measuredStretchSpanPosition.Count)
                        {
                            if (!IsStretchSpanVisible(_measuredStretchSpanPosition[startIndex].start,
                                    _measuredStretchSpanPosition[startIndex].end))
                            {
                                startIndex++;
                            }
                            else
                            {
                                break;
                            }
                        }

                        // 从先前最后一个可见行列开始找不可见的行列索引
                        endIndex = Mathf.Max(startIndex, endIndex);
                        while (endIndex + 1 < _measuredStretchSpanPosition.Count)
                        {
                            if (IsStretchSpanVisible(_measuredStretchSpanPosition[endIndex + 1].start,
                                    _measuredStretchSpanPosition[endIndex + 1].end))
                            {
                                endIndex++;
                            }
                            else
                            {
                                break;
                            }
                        }

                        break;
                    }
                    case < 0:
                    default:
                    {
                        // 从先前最后一个可见行列开始找再次可见的行列索引
                        while (endIndex >= 0)
                        {
                            if (!IsStretchSpanVisible(_measuredStretchSpanPosition[endIndex].start,
                                    _measuredStretchSpanPosition[endIndex].end))
                            {
                                endIndex--;
                            }
                            else
                            {
                                break;
                            }
                        }

                        // 从先前第一个可见子项开始找不可见的子项索引
                        startIndex = Mathf.Min(endIndex, startIndex);
                        while (startIndex - 1 >= 0)
                        {
                            if (IsStretchSpanVisible(_measuredStretchSpanPosition[startIndex - 1].start,
                                    _measuredStretchSpanPosition[startIndex - 1].end))
                            {
                                startIndex--;
                            }
                            else
                            {
                                break;
                            }
                        }

                        break;
                    }
                }

                return new LayoutInScreenCacheData
                {
                    Start = Mathf.Clamp(startIndex, 0, _measuredStretchSpanPosition.Count - 1),
                    End = Mathf.Clamp(endIndex, 0, _measuredStretchSpanPosition.Count - 1),
                };
            }
        }

        private void LayoutVisibleViewHolders(Recycler recycler)
        {
            if (_layoutCacheData is not LayoutInScreenCacheData inScreenData)
            {
                return;
            }

            var stretchOffset = orientation switch
            {
                Orientation.Horizontal => ViewportSize.x / 2 + _scrollPosition,
                Orientation.Vertical => -(ViewportSize.y / 2 + _scrollPosition),
                _ => 0f,
            };
            var spanOffset = orientation switch
            {
                Orientation.Horizontal => -ViewportSize.y / 2,
                Orientation.Vertical => ViewportSize.x / 2,
                _ => 0f,
            };
            for (var stretchIndex = inScreenData.Start; stretchIndex <= inScreenData.End; stretchIndex++)
            {
                if (!_measuredStretchSpanViewHolders.TryGetValue(stretchIndex, out var viewHolderIndexes))
                {
                    return;
                }

                viewHolderIndexes.ForEach(i =>
                {
                    var viewHolder = recycler.GetViewHolderForPosition(i);
                    if (!viewHolder)
                    {
                        return;
                    }

                    // 设置子项位置和尺寸
                    var layout = _measuredViewHolderPositions[i];
                    viewHolder.RectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                    viewHolder.RectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                    viewHolder.RectTransform.pivot = new Vector2(0.5f, 0.5f);
                    viewHolder.RectTransform.anchoredPosition = orientation switch
                    {
                        Orientation.Horizontal => new Vector2(layout.position.x - stretchOffset,
                            -layout.position.y - spanOffset),
                        Orientation.Vertical => new Vector2(layout.position.x - spanOffset,
                            -layout.position.y - stretchOffset),
                        _ => Vector2.zero,
                    };
                    viewHolder.RectTransform.sizeDelta = layout.size;

                    // 展示子项
                    recycler.ShowViewHolder(viewHolder);
                });
            }
        }

        private bool IsStretchSpanVisible(float start, float end)
        {
            var viewportSize = orientation switch
            {
                Orientation.Horizontal => ViewportSize.x,
                Orientation.Vertical => ViewportSize.y,
                _ => 0f,
            };
            return (start >= _scrollPosition && start < _scrollPosition + viewportSize) ||
                   (end > _scrollPosition && end <= _scrollPosition + viewportSize);
        }

        private bool IsStretchSpanFullVisible(float start, float end)
        {
            var viewportSize = orientation switch
            {
                Orientation.Horizontal => ViewportSize.x,
                Orientation.Vertical => ViewportSize.y,
                _ => 0f,
            };
            return !(start < _scrollPosition || end > _scrollPosition + viewportSize);
        }

        private (bool above, float distance) IsStretchSpanAboveScreen(float end)
        {
            return (end <= _scrollPosition, end - _scrollPosition);
        }

        private (bool below, float distance) IsStretchSpanBelowScreen(float start)
        {
            var viewportSize = orientation switch
            {
                Orientation.Horizontal => ViewportSize.x,
                Orientation.Vertical => ViewportSize.y,
                _ => 0f,
            };
            return (start >= _scrollPosition + viewportSize, start - _scrollPosition - viewportSize);
        }

        private class DefaultSpanSizeLookup : ISpanSizeLookup
        {
            public int GetSpanSize(int position)
            {
                return 1;
            }
        }

        public interface ISpanSizeLookup
        {
            int GetSpanSize(int position);
        }
    }
}