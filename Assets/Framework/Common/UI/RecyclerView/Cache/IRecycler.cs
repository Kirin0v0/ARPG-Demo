namespace Framework.Common.UI.RecyclerView.Cache
{
    public interface IRecyclerQuery
    {
        RecyclerViewHolder[] GetVisibleViewHolders();
        RecyclerViewHolder GetVisibleViewHolderForPosition(int position);
        RecyclerViewHolder GetViewHolderForPosition(int position);
    }
}