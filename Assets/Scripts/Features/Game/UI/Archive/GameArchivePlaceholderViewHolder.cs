using Features.Game.Data;
using Framework.Common.UI.RecyclerView;
using UnityEngine.EventSystems;

namespace Features.Game.UI.Archive
{
    public class GameArchivePlaceholderViewHolder : RecyclerViewHolder
    {
        private RecyclerViewHolderSelectable _selectable;
        private GameArchivePlaceholderUIData _data;
        private System.Action<RecyclerViewHolder, MoveDirection> _navigationMoveCallback;

        public void Init()
        {
            _selectable = GetComponent<RecyclerViewHolderSelectable>();
            _selectable.OnNavigationSelect += OnSelect;
            _selectable.OnNavigationDeselect += OnDeselect;
            _selectable.OnNavigationMove += OnMove;
        }

        public void Bind(
            GameArchivePlaceholderUIData data,
            System.Action<RecyclerViewHolder, MoveDirection> navigationMoveCallback
        )
        {
            _data = data;
            _navigationMoveCallback = navigationMoveCallback;
        }

        public void Show()
        {
            if (_data.Selected)
            {
                EventSystem.current?.SetSelectedGameObject(gameObject);
            }
        }

        public void Hide()
        {
            if (_data.Selected)
            {
                EventSystem.current?.SetSelectedGameObject(null);
            }
        }

        public void Unbind()
        {
            _data = null;
            _navigationMoveCallback = null;
        }

        public void Destroy()
        {
            _selectable.OnNavigationSelect -= OnSelect;
            _selectable.OnNavigationDeselect -= OnDeselect;
            _selectable.OnNavigationMove -= OnMove;
            _selectable = null;
        }

        private void OnSelect()
        {
            _data.Selected = true;
        }

        private void OnDeselect()
        {
            _data.Selected = false;
        }

        private void OnMove(MoveDirection moveDirection)
        {
            _navigationMoveCallback?.Invoke(this, moveDirection);
        }
    }
}