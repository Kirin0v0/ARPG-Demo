using Features.Game.Data;
using Framework.Common.UI.RecyclerView;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Features.Game.UI.Buff
{
    [RequireComponent(typeof(RecyclerViewHolderSelectable))]
    public class GameBuffDetailItemViewHolder : RecyclerViewHolder
    {
        [SerializeField] private Image imgIcon;
        [SerializeField] private TextMeshProUGUI textName;
        [SerializeField] private TextMeshProUGUI textInfo;

        private RecyclerViewHolderSelectable _selectable;
        public GameBuffDetailUIData Data { private set; get; }
        private System.Action<RecyclerViewHolder, MoveDirection> _navigationMoveCallback;

        public void Init()
        {
            _selectable = GetComponent<RecyclerViewHolderSelectable>();
            _selectable.OnNavigationSelect += OnSelect;
            _selectable.OnNavigationDeselect += OnDeselect;
            _selectable.OnNavigationMove += OnMove;
        }

        public void Bind(
            GameBuffDetailUIData data,
            System.Action<RecyclerViewHolder, MoveDirection> navigationMoveCallback
        )
        {
            Data = data;
            _navigationMoveCallback = navigationMoveCallback;

            imgIcon.sprite = data.Icon;
            textName.text = data.Name + (data.MaxStack > 1 ? $"({data.Stack}层)" : "");
            textInfo.text = !data.Permanent ? $"剩余时间: {data.Duration.ToString("F1")}秒" : "永久存在";
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