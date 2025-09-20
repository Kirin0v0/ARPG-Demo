using Features.Game.Data;
using Framework.Common.Debug;
using Framework.Common.UI.RecyclerView;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Features.Game.UI.Package
{
    [RequireComponent(typeof(RecyclerViewHolderSelectable))]
    public class GamePackageGridPlaceholderViewHolder : RecyclerViewHolder
    {
        [Title("UI关联")] [SerializeField] private Image imgHighlighted;
        [SerializeField] private Image imgFocused;

        private RecyclerViewHolderSelectable _selectable;
        private GamePackagePlaceholderUIData _data;
        private System.Action<RecyclerViewHolder, MoveDirection> _navigationMoveCallback;

        public void Init()
        {
            _selectable = GetComponent<RecyclerViewHolderSelectable>();
            _selectable.OnSelectionStateNormal += OnStateNormal;
            _selectable.OnSelectionStateHighlighted += OnStateHighlighted;
            _selectable.OnSelectionStatePressed += OnStateHighlighted;
            _selectable.OnSelectionStateSelected += OnStateSelected;
            _selectable.OnNavigationSelect += OnSelect;
            _selectable.OnNavigationDeselect += OnDeselect;
            _selectable.OnNavigationMove += OnMove;
        }

        public void Bind(
            GamePackagePlaceholderUIData placeholderUIData,
            System.Action<RecyclerViewHolder, MoveDirection> navigationMoveCallback
        )
        {
            _data = placeholderUIData;
            _navigationMoveCallback = navigationMoveCallback;
        }

        public void Show()
        {
            if (_data.Focused)
            {
                EventSystem.current?.SetSelectedGameObject(gameObject);
            }
        }

        public void Hide()
        {
            if (_data.Focused)
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
            _selectable.OnSelectionStateNormal -= OnStateNormal;
            _selectable.OnSelectionStateHighlighted -= OnStateHighlighted;
            _selectable.OnSelectionStatePressed -= OnStateHighlighted;
            _selectable.OnSelectionStateSelected -= OnStateSelected;
            _selectable.OnNavigationSelect -= OnSelect;
            _selectable.OnNavigationDeselect -= OnDeselect;
            _selectable.OnNavigationMove -= OnMove;
            _selectable = null;
        }

        private void OnStateNormal()
        {
            imgHighlighted.gameObject.SetActive(false);
            imgFocused.gameObject.SetActive(false);
        }

        private void OnStateHighlighted()
        {
            imgHighlighted.gameObject.SetActive(true);
            imgFocused.gameObject.SetActive(false);
        }

        private void OnStateSelected()
        {
            imgHighlighted.gameObject.SetActive(false);
            imgFocused.gameObject.SetActive(true);
        }

        private void OnSelect()
        {
            if (_data != null)
            {
                _data.Focused = true;
            }
        }

        private void OnDeselect()
        {
            if (_data != null)
            {
                _data.Focused = false;
            }
        }

        private void OnMove(MoveDirection moveDirection)
        {
            _navigationMoveCallback?.Invoke(this, moveDirection);
        }
    }
}