using Features.Main.Data;
using Framework.Common.UI.RecyclerView;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Features.Main.Archive
{
    [RequireComponent(typeof(RecyclerViewHolderSelectable))]
    public class MainArchiveItemViewHolder : RecyclerViewHolder
    {
        [SerializeField] private TextMeshProUGUI textId;
        [SerializeField] private TextMeshProUGUI textAuto;
        [SerializeField] private TextMeshProUGUI textPlayerName;
        [SerializeField] private TextMeshProUGUI textPlayerLevel;
        [SerializeField] private TextMeshProUGUI textSaveTime;
        [SerializeField] private TextMeshProUGUI textPlayTime;
        [SerializeField] private TextMeshProUGUI textMapName;
        [SerializeField] private TextMeshProUGUI textQuestName;

        private RecyclerViewHolderSelectable _selectable;
        private MainArchiveItemUIData _data;
        private System.Action<RecyclerViewHolder, MoveDirection> _navigationMoveCallback;
        private System.Action<MainArchiveItemUIData> _clickCallback;
        private System.Action<MainArchiveItemUIData> _deleteCallback;

        public void Init()
        {
            _selectable = GetComponent<RecyclerViewHolderSelectable>();
            _selectable.OnNavigationSelect += OnSelect;
            _selectable.OnNavigationDeselect += OnDeselect;
            _selectable.OnNavigationMove += OnMove;
            _selectable.onClick.AddListener(ClickItem);
        }

        public void Bind(
            MainArchiveItemUIData data,
            System.Action<RecyclerViewHolder, MoveDirection> navigationMoveCallback,
            System.Action<MainArchiveItemUIData> clickCallback,
            System.Action<MainArchiveItemUIData> deleteCallback
        )
        {
            _data = data;
            _navigationMoveCallback = navigationMoveCallback;
            _clickCallback = clickCallback;
            _deleteCallback = deleteCallback;

            textId.text = data.Id.ToString();
            textAuto.gameObject.SetActive(data.Auto);
            textPlayerName.text = data.PlayerName;
            textPlayerLevel.text = $"Lv.{data.PlayerLevel}";
            textSaveTime.text = data.SaveTime;
            textPlayTime.text = data.PlayTime;
            textMapName.text = data.MapName;
            textQuestName.text = data.QuestName;
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

        public void DeleteItem()
        {
            _deleteCallback?.Invoke(_data);
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
            _deleteCallback = null;
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