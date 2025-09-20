namespace Framework.Common.UI.RecyclerView.Adapter
{
    public abstract class RecyclerViewAdapterDataObserver
    {
        public virtual void OnChanged()
        {
        }

        public virtual void OnItemRangeChanged(int positionStart, int itemCount)
        {
        }

        public virtual void OnItemRangeChanged(int positionStart, int itemCount, object payload)
        {
            OnItemRangeChanged(positionStart, itemCount);
        }

        // public virtual void OnItemRangeInserted(int positionStart, int itemCount)
        // {
        // }
        //
        // public virtual void OnItemRangeRemoved(int positionStart, int itemCount)
        // {
        // }
        //
        // public virtual void OnItemRangeMoved(int fromPosition, int toPosition, int itemCount)
        // {
        // }
    }
}