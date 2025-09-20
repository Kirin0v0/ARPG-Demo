using Features.Game.Data;
using Framework.Common.UI.RecyclerView;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Features.Game.UI.Dialogue
{
    [RequireComponent(typeof(RecyclerViewHolderSelectable))]
    public class GameDialogueOptionItemViewHolder : RecyclerViewHolder
    {
        [SerializeField] private TextMeshProUGUI textMessage;

        private RecyclerViewHolderSelectable _selectable;
        public GameDialogueOptionUIData Data { private set; get; }
        private System.Action<RecyclerViewHolder, MoveDirection> _navigationMoveCallback;
        private System.Action<RecyclerViewHolder> _clickCallback;

        public void Init()
        {
            _selectable = GetComponent<RecyclerViewHolderSelectable>();
            _selectable.OnNavigationSelect += OnSelect;
            _selectable.OnNavigationDeselect += OnDeselect;
            _selectable.OnNavigationMove += OnMove;
            _selectable.onClick.AddListener(ClickItem);
        }

        public void Bind(GameDialogueOptionUIData data)
        {
            Data = data;
            _navigationMoveCallback = data.OnNavigationMoved;
            _clickCallback = data.OnClicked;

            textMessage.text = data.Message;
        }

        public void Show()
        {
            if (Data.Focused)
            {
                EventSystem.current?.SetSelectedGameObject(gameObject);
            }
        }

        public void Hide()
        {
            if (Data.Focused)
            {
                EventSystem.current?.SetSelectedGameObject(null);
            }
        }

        public void Unbind()
        {
            Data = null;
            _navigationMoveCallback = null;
            _clickCallback = null;
        }

        public void Destroy()
        {
            _selectable.onClick.RemoveListener(ClickItem);
            _selectable.OnNavigationSelect -= OnSelect;
            _selectable.OnNavigationDeselect -= OnDeselect;
            _selectable.OnNavigationMove -= OnMove;
            _selectable = null;
        }

        private void ClickItem()
        {
            _clickCallback?.Invoke(this);
        }

        private void OnSelect()
        {
            if (Data != null)
            {
                Data.Focused = true;
            }
        }

        private void OnDeselect()
        {
            if (Data != null)
            {
                Data.Focused = false;
            }
        }

        private void OnMove(MoveDirection moveDirection)
        {
            _navigationMoveCallback?.Invoke(this, moveDirection);
        }
    }
}