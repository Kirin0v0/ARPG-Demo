using Framework.Common.UI.RecyclerView;
using UnityEngine.EventSystems;

namespace Features.Game.Data
{
    public class GameDialogueOptionUIData
    {
        public int Index;
        public string Message;
        public bool Focused; // 是否聚焦
        public System.Action<RecyclerViewHolder, MoveDirection> OnNavigationMoved;
        public System.Action<RecyclerViewHolder> OnClicked;
    }
}