using System.Collections.Generic;
using Framework.Common.UI.RecyclerView.Cache;
using Framework.Common.UI.RecyclerView.Scroller;
using UnityEngine;

namespace Framework.Common.UI.RecyclerView.LayoutManager
{
    public class RecyclerViewCircleLayoutManager : RecyclerViewLayoutManager
    {
        public enum Orientation
        {
            Clockwise,
            CounterClockwise,
        }

        [Header("布局相关")] [SerializeField] private Orientation orientation;
        public Orientation GetOrientation() => orientation;
        [SerializeField] private bool useCustomRadius;
        [Min(0)] [SerializeField] private float customRadius;
        [SerializeField] private float initialAngle;
        [SerializeField] private bool useCustomIntervalAngle;
        [Min(0)] [SerializeField] private float customIntervalAngle;

        [Header("滚动相关")] [SerializeField] private bool scroll = true;
        [SerializeField] private bool snap = false;
        [Min(0f)] [SerializeField] private float dragSpeed = 0.1f;
        [Min(0f)] [SerializeField] private float wheelSpeed = 1f;
        [Min(0f)] [SerializeField] private float minSmoothScrollTime = 0.05f;
        [Min(0f)] [SerializeField] private float maxSmoothScrollTime = 0.5f;

        private RecyclerViewCircleScroller _scroller;
        private float _scrollPosition;
        private float _radius;
        private float _intervalAngle;
        public override bool Scrolling => _scroller.Scrolling;

        private readonly Dictionary<int, float> _viewHolderMeasuredAngles = new();
        private bool _layouted = false;

        protected override void OnAttachToRecyclerView(RecyclerView recyclerView)
        {
            if (scroll)
            {
                var olderScrolleres = recyclerView.Content.GetComponents<RecyclerViewScroller>();
                foreach (var scroller in olderScrolleres)
                {
                    Destroy(scroller);
                }

                _scroller = recyclerView.Content.gameObject.AddComponent<RecyclerViewCircleScroller>();
                _scroller.Center = new Vector2(RecyclerView.Content.position.x, RecyclerView.Content.position.y) +
                                   RecyclerView.Content.rect.center;
                _scroller.MinPosition = float.MinValue;
                _scroller.MaxPosition = float.MaxValue;
                _scroller.Snap = snap;
                _scroller.DragSpeed = dragSpeed;
                _scroller.WheelSpeed = wheelSpeed;
                _scroller.PositionChangedEvent += OnScrollPositionChanged;
            }

            _scrollPosition = 0f;
            _radius = 0f;
            _intervalAngle = 0f;
            _viewHolderMeasuredAngles.Clear();
        }

        protected override void OnMeasure(Recycler recycler)
        {
            _viewHolderMeasuredAngles.Clear();
            _layouted = false;

            var adapter = RecyclerView.Adapter;
            if (!adapter)
            {
                return;
            }

            var itemCount = adapter.GetItemCount();
            var angle = initialAngle + _scrollPosition;
            _intervalAngle = useCustomIntervalAngle ? customIntervalAngle : 360f / Mathf.Max(1, itemCount);
            var contentRadius = Mathf.Min(ContentSize.x / 2, ContentSize.y / 2);
            var defaultRadius = contentRadius;

            for (int i = 0; i < itemCount; i++)
            {
                var viewType = adapter.GetItemViewType(i);
                var viewHolderTemplate = adapter.GetViewHolderTemplate(viewType);
                var size = adapter.MeasureViewHolderTemplate(viewHolderTemplate, i, ViewportSize);

                if (i != 0)
                {
                    angle = orientation switch
                    {
                        Orientation.Clockwise => angle - _intervalAngle,
                        Orientation.CounterClockwise => angle + _intervalAngle,
                    };
                }

                _viewHolderMeasuredAngles.Add(i, angle);

                defaultRadius = Mathf.Min(defaultRadius, contentRadius - size.x / 2, contentRadius - size.y / 2);
            }

            _radius = useCustomRadius ? customRadius : defaultRadius;

            if (scroll)
            {
                _scroller.Interval = _intervalAngle;
            }
        }

        protected override void OnLayoutChildren(Recycler recycler)
        {
            var adapter = RecyclerView.Adapter;
            if (!adapter)
            {
                return;
            }

            // 如果还没布局过，就清除所有可见子项ViewHolder
            if (!_layouted)
            {
                var viewHolders = recycler.GetVisibleViewHolders();
                foreach (var viewHolder in viewHolders)
                {
                    recycler.HideViewHolder(viewHolder);
                    recycler.RecycleViewHolder(viewHolder);
                }
            }

            // 重新对所有子项进行布局
            var itemCount = adapter.GetItemCount();
            for (var i = 0; i < itemCount; i++)
            {
                var viewHolder = recycler.GetViewHolderForPosition(i);
                if (!viewHolder)
                {
                    continue;
                }

                var angle = _viewHolderMeasuredAngles[i];
                viewHolder.RectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                viewHolder.RectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                viewHolder.RectTransform.pivot = new Vector2(0.5f, 0.5f);
                viewHolder.RectTransform.anchoredPosition = CalculateItemPosition(angle);

                recycler.ShowViewHolder(viewHolder);
            }

            return;

            Vector2 CalculateItemPosition(float angle)
            {
                var radian = angle * (Mathf.PI / 180f);
                return new Vector2(_radius * Mathf.Cos(radian), _radius * Mathf.Sin(radian));
            }
        }

        protected override void OnDetachFromRecyclerView(RecyclerView recyclerView)
        {
            if (scroll)
            {
                Destroy(_scroller);
                _scroller = null;
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

            var scrollAngle = position - _scrollPosition;
            var scrollAngleRatio = Mathf.Min(1f, Mathf.Abs(scrollAngle / 360f));
            var scrollTime = scrollAngleRatio * (maxSmoothScrollTime - minSmoothScrollTime) + minSmoothScrollTime;
            _scroller.SmoothScrollToPosition(position, scrollAngle / scrollTime);
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
            if (!_viewHolderMeasuredAngles.TryGetValue(index, out var indexAngle) ||
                !_viewHolderMeasuredAngles.TryGetValue(0, out var pivotAngle))
            {
                return;
            }

            var target = CalculateMinRotationAngle(indexAngle, pivotAngle);
            if (smoothScroll)
            {
                SmoothScrollToPosition(target);
            }
            else
            {
                ScrollToPosition(target);
            }

            float CalculateMinRotationAngle(float angle1, float angle2)
            {
                angle1 = angle1 % 360;
                angle2 = angle2 % 360;
                var difference = angle2 - angle1;

                if (difference > 180)
                {
                    difference -= 360;
                }
                else if (difference < -180)
                {
                    difference += 360;
                }

                return difference;
            }
        }

        public override bool IsViewHolderVisible(int position) => true;

        private void OnScrollPositionChanged(float position)
        {
            _scrollPosition = position;
            RecyclerView.Refresh();
            OnScrolled?.Invoke();
        }
    }
}