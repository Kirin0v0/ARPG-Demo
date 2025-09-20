using Features.Game.Data;
using Framework.Common.UI.RecyclerView;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Features.Game.UI.BattleCommand
{
    [RequireComponent(typeof(RecyclerViewHolderSelectable))]
    public class GameBattleCommandListGroupViewHolder : RecyclerViewHolder
    {
        [SerializeField] private TextMeshProUGUI textName;
        [SerializeField] private Image imgBackgroundEnable;
        [SerializeField] private Image imgBackgroundDisable;
        
        private RecyclerViewHolderSelectable _selectable;
        private GameBattleCommandGroupUIData _data;
        private System.Action<RecyclerViewHolder, MoveDirection> _navigationMoveCallback;
        private System.Action<object> _clickCallback;
        
        public void Init()
        {
            _selectable = GetComponent<RecyclerViewHolderSelectable>();
            _selectable.OnNavigationSelect += OnSelect;
            _selectable.OnNavigationDeselect += OnDeselect;
            _selectable.OnNavigationMove += OnMove;
            _selectable.onClick.AddListener(ClickItem);
        }

        public void Bind(
            GameBattleCommandGroupUIData groupUIData,
            System.Action<RecyclerViewHolder, MoveDirection> navigationMoveCallback,
            System.Action<object> clickCallback
        )
        {
            _data = groupUIData;
            _navigationMoveCallback = navigationMoveCallback;
            _clickCallback = clickCallback;
            
            textName.text = groupUIData.Name;
            imgBackgroundEnable.gameObject.SetActive(groupUIData.Enable);
            imgBackgroundDisable.gameObject.SetActive(!groupUIData.Enable);
        }

        public void Show()
        {
            if (_data.Selected)
            {
                EventSystem.current?.SetSelectedGameObject(gameObject);
            }
        }

        private void ClickItem()
        {
            _clickCallback?.Invoke(_data);
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
            _clickCallback = null;
        }

        public void Destroy()
        {
            _selectable.OnNavigationSelect -= OnSelect;
            _selectable.OnNavigationDeselect -= OnDeselect;
            _selectable.OnNavigationMove -= OnMove;
            _selectable.onClick.RemoveListener(ClickItem);
            _selectable = null;
        }

        private void OnSelect()
        {
            if (_data != null)
            {
                _data.Selected = true;
            }
        }

        private void OnDeselect()
        {
            if (_data != null)
            {
                _data.Selected = false;
            }
        }

        private void OnMove(MoveDirection moveDirection)
        {
            _navigationMoveCallback?.Invoke(this, moveDirection);
        }
    }
}