using Features.Game.Data;
using Framework.Common.Debug;
using Framework.Common.UI.RecyclerView;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Features.Game.UI.Character
{
    [RequireComponent(typeof(RecyclerViewHolderSelectable))]
    public class GameCharacterSkillItemViewHolder : RecyclerViewHolder
    {
        [SerializeField] private TextMeshProUGUI text;

        private RecyclerViewHolderSelectable _selectable;
        public GameCharacterSkillUIData Data { private set; get; }
        private System.Action<RecyclerViewHolder, MoveDirection> _navigationMoveCallback;

        public void Init()
        {
            _selectable = GetComponent<RecyclerViewHolderSelectable>();
            _selectable.OnNavigationSelect += OnSelect;
            _selectable.OnNavigationDeselect += OnDeselect;
            _selectable.OnNavigationMove += OnMove;
        }

        public void Bind(
            GameCharacterSkillUIData data,
            System.Action<RecyclerViewHolder, MoveDirection> navigationMoveCallback
        )
        {
            Data = data;
            _navigationMoveCallback = navigationMoveCallback;
            text.text = data.Skill.flow.Name;
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