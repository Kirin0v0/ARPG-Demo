using System;
using System.Collections;
using Framework.Common.Debug;
using Framework.Common.UI.RecyclerView.Adapter;
using Framework.Common.UI.RecyclerView.Cache;
using Framework.Common.UI.RecyclerView.LayoutManager;
using Framework.Common.UI.RecyclerView.Scroller;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Framework.Common.UI.RecyclerView
{
    public class RecyclerView : MonoBehaviour
    {
        public const int NoPosition = -1;
        public const int InvalidType = -1;

        private RecyclerViewAdapter _adapter;

        public RecyclerViewAdapter Adapter
        {
            get => _adapter;
            set
            {
                SetAdapterInternal(value);
                Refresh();
                return;

                void SetAdapterInternal(RecyclerViewAdapter newAdapter)
                {
                    _adapter?.UnregisterAdapterDataObserver(_observer);
                    _adapter?.DetachFromRecyclerView(this);
                    _recycler.Clear();

                    _adapter = newAdapter;
                    _adapter?.RegisterAdapterDataObserver(_observer);
                    OnAdapterChanged?.Invoke(_adapter);
                    _adapter?.AttachToRecyclerView(this);
                }
            }
        }

        public UnityAction<RecyclerViewAdapter> OnAdapterChanged;

        private RecyclerViewLayoutManager _layoutManager;

        public RecyclerViewLayoutManager LayoutManager
        {
            get => _layoutManager;
            set
            {
                SetLayoutManagerInternal(value);
                Refresh();
                return;

                void SetLayoutManagerInternal(RecyclerViewLayoutManager newLayoutManager)
                {
                    StopSmoothScroll();
                    if (_layoutManager)
                    {
                        _layoutManager.DetachFromRecyclerView(this);
                    }

                    _recycler.Clear();
                    Content.DetachChildren();
                    _layoutManager = newLayoutManager;
                    OnLayoutManagerChanged?.Invoke(_layoutManager);

                    if (_layoutManager)
                    {
                        _layoutManager.AttachToRecyclerView(this);
                    }
                }
            }
        }

        public UnityAction<RecyclerViewLayoutManager> OnLayoutManagerChanged;

        public bool Scrolling => LayoutManager && LayoutManager.Scrolling;

        [SerializeField] private RectTransform content;
        public RectTransform Content => content;

        [Header("更新设置")] [SerializeField]
        private bool resetScrollPositionWhenDataSetChanged = true; // 是否在数据集改变时重置滚动位置，即数据整体刷新时回到列表起始位置

        [SerializeField] private bool remeasureWhenItemChanged = false; // 是否在子项单项改变时重新测量全部子项尺寸，这里仅在子项数据更新时会改变子项尺寸时需要

        private Recycler _recycler;
        public IRecyclerQuery RecyclerQuery => _recycler;

        private RecyclerViewDataObserver _observer;

        private bool _initialized;

        private void Awake()
        {
            Init();
        }

        public void Init()
        {
            if (_initialized)
            {
                return;
            }

            _initialized = true;
            _recycler = new Recycler(this);
            _observer = new RecyclerViewDataObserver(this);
        }

        public void Refresh()
        {
            if (!LayoutManager)
            {
                return;
            }

            LayoutManager.Measure(_recycler);
            LayoutManager.Layout(_recycler);
        }

        public void RequestLayout()
        {
            if (!LayoutManager)
            {
                return;
            }

            LayoutManager.Layout(_recycler);
        }

        public void ScrollToPosition(float position)
        {
            if (!LayoutManager)
            {
                return;
            }

            LayoutManager.ScrollToPosition(position);
        }

        public void SmoothScrollToPosition(float position)
        {
            if (!LayoutManager)
            {
                return;
            }

            LayoutManager.SmoothScrollToPosition(position);
        }

        public void StopSmoothScroll()
        {
            if (!LayoutManager)
            {
                return;
            }

            LayoutManager.StopSmoothScroll();
        }

        public void ScrollDelta(float delta)
        {
            if (!LayoutManager)
            {
                return;
            }

            LayoutManager.ScrollDelta(delta);
        }

        public void FocusItem(int index, bool smoothScroll)
        {
            if (!LayoutManager)
            {
                return;
            }

            LayoutManager.FocusItem(index, smoothScroll);
        }

        private void ProcessDataSetCompletelyChanged()
        {
            _recycler.MarkVisibleAndCachedViewHoldersNeedUpdate();
        }

        private void MarkViewHoldersNeedUpdate(int startPosition, int itemCount)
        {
            for (var i = startPosition; i < startPosition + itemCount; i++)
            {
                var viewHolder = _recycler.GetVisibleOrCachedViewHolderForPosition(i);
                if (viewHolder)
                {
                    viewHolder.AddFlags(RecyclerViewHolder.FlagUpdate);
                }
            }
        }

        private class RecyclerViewDataObserver : RecyclerViewAdapterDataObserver
        {
            private readonly RecyclerView _recyclerView;

            public RecyclerViewDataObserver(RecyclerView recyclerView)
            {
                _recyclerView = recyclerView;
            }

            public override void OnChanged()
            {
                base.OnChanged();
                _recyclerView.ProcessDataSetCompletelyChanged();
                if (_recyclerView.resetScrollPositionWhenDataSetChanged)
                {
                    _recyclerView.ScrollToPosition(0f);
                }

                _recyclerView.Refresh();
            }

            public override void OnItemRangeChanged(int positionStart, int itemCount)
            {
                base.OnItemRangeChanged(positionStart, itemCount);
                // 如果需要重新测量子项，这里就直接刷新全部数据集重新测量和布局，否则就仅给对应子项添加更新标识重新布局
                if (_recyclerView.remeasureWhenItemChanged)
                {
                    _recyclerView.ProcessDataSetCompletelyChanged();
                    _recyclerView.Refresh();
                }
                else
                {
                    _recyclerView.MarkViewHoldersNeedUpdate(positionStart, itemCount);
                    _recyclerView.RequestLayout();
                }
            }
        }

        private void OnValidate()
        {
            // 设置Canvas和Graphics Raycaster，用于动静分离
            if (!gameObject.TryGetComponent<Canvas>(out var canvas))
            {
                canvas = gameObject.AddComponent<Canvas>();
                canvas.overridePixelPerfect = true;
            }

            if (!gameObject.TryGetComponent<GraphicRaycaster>(out var graphicRaycaster))
            {
                graphicRaycaster = gameObject.AddComponent<GraphicRaycaster>();
            }

            // 创建子内容
            if (!content)
            {
                if (transform.childCount > 0)
                {
                    // 固定采用子物体的RectTransform组件
                    var child = transform.GetChild(0);
                    content = child.GetComponent<RectTransform>();
                }
                else
                {
                    // 如果没有子物体则创建子物体
                    var child = new GameObject("Content");
                    child.transform.SetParent(transform, false);
                    child.AddComponent<Image>();
                    child.AddComponent<RectMask2D>();
                    content = child.GetComponent<RectTransform>();
                    if (!content)
                    {
                        content = child.AddComponent<RectTransform>();
                    }

                    content.anchorMin = Vector2.zero;
                    content.anchorMax = Vector2.one;
                    content.offsetMin = Vector2.zero;
                    content.offsetMax = Vector2.zero;
                    content.pivot = new Vector2(0.5f, 0.5f);
                }
            }
        }
    }
}