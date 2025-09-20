using Framework.Common.UI.RecyclerView.Cache;
using UnityEngine;
using UnityEngine.Events;

namespace Framework.Common.UI.RecyclerView.LayoutManager
{
    public abstract class RecyclerViewLayoutManager : MonoBehaviour
    {
        private RecyclerView _recyclerView;

        protected RecyclerView RecyclerView
        {
            get => _recyclerView;
            set
            {
                _recyclerView = value;
                DefaultMeasure(value);
            }
        }

        public Vector2 ViewportSize { get; protected set; } = Vector2.zero; // 视窗大小，展示窗口的尺寸
        public Vector2 ContentSize { get; protected set; } = Vector2.zero; // 内容大小，实际子项列表的尺寸

        public bool InMeasuring { get; private set; } = false;
        public bool InLayouting { get; private set; } = false;
        public virtual bool Scrolling => false;

        public UnityAction OnMeasureCompleted;
        public UnityAction OnLayoutCompleted;
        public UnityAction OnScrolled;

        public void AttachToRecyclerView(RecyclerView recyclerView)
        {
            RecyclerView = recyclerView;
            OnAttachToRecyclerView(recyclerView);
        }

        public void Measure(Recycler recycler)
        {
            InMeasuring = true;
            DefaultMeasure(_recyclerView);
            OnMeasure(recycler);
            InMeasuring = false;
            OnMeasureCompleted?.Invoke();
        }

        public void Layout(Recycler recycler)
        {
            InLayouting = true;
            recycler.HideAndRecyclerInvalidViewHolders();
            OnLayoutChildren(recycler);
            InLayouting = false;
            OnLayoutCompleted?.Invoke();
        }

        public void DetachFromRecyclerView(RecyclerView recyclerView)
        {
            RecyclerView = null;
            OnDetachFromRecyclerView(recyclerView);
        }

        public abstract void ScrollToPosition(float position);

        public abstract void SmoothScrollToPosition(float position);

        public abstract void StopSmoothScroll();

        public abstract void ScrollDelta(float delta);

        public abstract void FocusItem(int index, bool smoothScroll);

        public abstract bool IsViewHolderVisible(int position);

        protected abstract void OnAttachToRecyclerView(RecyclerView recyclerView);

        protected abstract void OnMeasure(Recycler recycler);

        protected abstract void OnLayoutChildren(Recycler recycler);

        protected abstract void OnDetachFromRecyclerView(RecyclerView recyclerView);

        private void DefaultMeasure(RecyclerView recyclerView)
        {
            if (recyclerView == null)
            {
                ViewportSize = Vector2.zero;
                ContentSize = Vector2.zero;
            }
            else
            {
                ViewportSize = recyclerView.Content.rect.size;
                ContentSize = recyclerView.Content.rect.size;
            }
        }
    }
}