using Features.Game.Data;
using Framework.Common.Debug;
using Framework.Common.UI.RecyclerView;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Features.Game.UI.Trade
{
    [RequireComponent(typeof(RecyclerViewHolderSelectable))]
    public class GameTradeGoodsItemViewHolder : RecyclerViewHolder
    {
        [SerializeField] private GameObject imgBackgroundEnable;
        [SerializeField] private GameObject imgBackgroundDisable;
        [SerializeField] private TextMeshProUGUI textName;
        [SerializeField] private TextMeshProUGUI textPrice;
        [SerializeField] private GameObject targetNumberLayout;
        [SerializeField] private Button btnDecreaseTargetNumber;
        [SerializeField] private TextMeshProUGUI textTargetNumber;
        [SerializeField] private Button btnIncreaseTargetNumber;
        [SerializeField] private TextMeshProUGUI textInventory;
        [SerializeField] private TextMeshProUGUI textCurrentNumber;

        private RecyclerViewHolderSelectable _selectable;
        public GameTradeGoodsUIData Data { private set; get; }
        private System.Action<RecyclerViewHolder> _decreaseTargetNumberCallback;
        private System.Action<RecyclerViewHolder> _increaseTargetNumberCallback;
        private System.Action<RecyclerViewHolder, MoveDirection> _navigationMoveCallback;

        public void Init()
        {
            _selectable = GetComponent<RecyclerViewHolderSelectable>();
            _selectable.OnNavigationSelect += OnSelect;
            _selectable.OnNavigationDeselect += OnDeselect;
            _selectable.OnNavigationMove += OnMove;
            btnDecreaseTargetNumber.onClick.AddListener(DecreaseTargetNumber);
            btnIncreaseTargetNumber.onClick.AddListener(IncreaseTargetNumber);
        }
        
        public void Bind(
            GameTradeGoodsUIData data,
            System.Action<RecyclerViewHolder> decreaseTargetNumberCallback,
            System.Action<RecyclerViewHolder> increaseTargetNumberCallback,
            System.Action<RecyclerViewHolder, MoveDirection> navigationMoveCallback
        )
        {
            Data = data;
            _decreaseTargetNumberCallback = decreaseTargetNumberCallback;
            _increaseTargetNumberCallback = increaseTargetNumberCallback;
            _navigationMoveCallback = navigationMoveCallback;

            if (data.Available)
            {
                imgBackgroundEnable.gameObject.SetActive(true);
                imgBackgroundDisable.gameObject.SetActive(false);
            }
            else
            {
                imgBackgroundEnable.gameObject.SetActive(false);
                imgBackgroundDisable.gameObject.SetActive(true);
            }

            textName.text = data.PackageName;
            textPrice.text = data.UnitPrice.ToString("F1");
            targetNumberLayout.gameObject.SetActive(data.Available && data.TargetNumber > 0);
            btnDecreaseTargetNumber.gameObject.SetActive(data.AllowDecreaseTargetNumber);
            btnIncreaseTargetNumber.gameObject.SetActive(data.AllowIncreaseTargetNumber);
            textTargetNumber.text = data.TargetNumber.ToString();
            textInventory.gameObject.SetActive(data.InventoryLimited);
            textInventory.text = data.Inventory.ToString();
            textCurrentNumber.text = data.HoldNumber.ToString();
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
            _decreaseTargetNumberCallback = null;
            _increaseTargetNumberCallback = null;
            _navigationMoveCallback = null;
        }

        public void Destroy()
        {
            btnDecreaseTargetNumber.onClick.RemoveListener(DecreaseTargetNumber);
            btnIncreaseTargetNumber.onClick.RemoveListener(IncreaseTargetNumber);
            _selectable.OnNavigationSelect -= OnSelect;
            _selectable.OnNavigationDeselect -= OnDeselect;
            _selectable.OnNavigationMove -= OnMove;
            _selectable = null;
        }

        private void DecreaseTargetNumber()
        {
            _decreaseTargetNumberCallback?.Invoke(this);
            EventSystem.current?.SetSelectedGameObject(gameObject);
        }

        private void IncreaseTargetNumber()
        {
            _increaseTargetNumberCallback?.Invoke(this);
            EventSystem.current?.SetSelectedGameObject(gameObject);
        }

        private void OnSelect()
        {
            if (Data != null && !Data.Focused)
            {
                Data.Focused = true;
                btnDecreaseTargetNumber.gameObject.SetActive(Data.AllowDecreaseTargetNumber);
                btnIncreaseTargetNumber.gameObject.SetActive(Data.AllowIncreaseTargetNumber);
                textTargetNumber.text = Data.TargetNumber.ToString();
            }
        }

        private void OnDeselect()
        {
            if (Data != null && Data.Focused)
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