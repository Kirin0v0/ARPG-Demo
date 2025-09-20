using Features.Game.Data;
using Framework.Common.UI.RecyclerView;
using Quest.Data;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Features.Game.UI.Quest
{
    [RequireComponent(typeof(RecyclerViewHolderSelectable))]
    public class GameQuestItemViewHolder : RecyclerViewHolder
    {
        [SerializeField] private TextMeshProUGUI textTitle;
        [SerializeField] private TextMeshProUGUI textInProgress;
        [SerializeField] private TextMeshProUGUI textAwaitSubmit;
        [SerializeField] private TextMeshProUGUI textCompleted;

        private RecyclerViewHolderSelectable _selectable;
        public GameQuestItemUIData Data { private set; get; }
        private System.Action<RecyclerViewHolder> _navigationSelectedCallback;
        private System.Action<RecyclerViewHolder> _navigationDeselectedCallback;
        private System.Action<RecyclerViewHolder, MoveDirection> _navigationMoveCallback;

        public void Init()
        {
            _selectable = GetComponent<RecyclerViewHolderSelectable>();
            _selectable.OnNavigationSelect += OnSelect;
            _selectable.OnNavigationDeselect += OnDeselect;
            _selectable.OnNavigationMove += OnMove;
        }

        public void Bind(
            GameQuestItemUIData data,
            System.Action<RecyclerViewHolder> navigationSelectedCallback,
            System.Action<RecyclerViewHolder> navigationDeselectedCallback,
            System.Action<RecyclerViewHolder, MoveDirection> navigationMoveCallback
        )
        {
            Data = data;
            _navigationSelectedCallback = navigationSelectedCallback;
            _navigationDeselectedCallback = navigationDeselectedCallback;
            _navigationMoveCallback = navigationMoveCallback;

            textTitle.text = data.Quest.info.title;
            textInProgress.gameObject.SetActive(false);
            textAwaitSubmit.gameObject.SetActive(false);
            textCompleted.gameObject.SetActive(false);
            if (data.Quest.state.IsQuestCompleted())
            {
                textCompleted.gameObject.SetActive(true);
            }
            else
            {
                if (data.Quest.state == QuestState.AllStepsComplete)
                {
                    textAwaitSubmit.gameObject.SetActive(true);
                }
                else
                {
                    textInProgress.gameObject.SetActive(true);
                }
            }
        }

        public void Show()
        {
            // 由于ViewHolder在Show和Hide时才会改变Active，而SetSelectedGameObject又在Active时才会发送事件，所以只能在这里设置Selected物体
            if (Data.Selected)
            {
                EventSystem.current?.SetSelectedGameObject(gameObject);
            }
        }

        public void Hide()
        {
            if (Data.Selected)
            {
                EventSystem.current?.SetSelectedGameObject(null);
            }
        }

        public void Unbind()
        {
            Data = null;
            _navigationSelectedCallback = null;
            _navigationDeselectedCallback = null;
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
                Data.Selected = true;
                _navigationSelectedCallback?.Invoke(this);
            }
        }

        private void OnDeselect()
        {
            if (Data != null)
            {
                Data.Selected = false;
                _navigationDeselectedCallback?.Invoke(this);
            }
        }

        private void OnMove(MoveDirection moveDirection)
        {
            _navigationMoveCallback?.Invoke(this, moveDirection);
        }
    }
}