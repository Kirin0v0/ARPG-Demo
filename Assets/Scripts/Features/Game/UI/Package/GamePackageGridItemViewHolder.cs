using Features.Game.Data;
using Framework.Common.UI.RecyclerView;
using Package;
using Package.Data;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.U2D;
using UnityEngine.UI;

namespace Features.Game.UI.Package
{
    [RequireComponent(typeof(RecyclerViewHolderSelectable))]
    public class GamePackageGridItemViewHolder : RecyclerViewHolder
    {
        [Title("UI关联")] [SerializeField] private Image imgBackground;
        [SerializeField] private Image imgBorder;
        [SerializeField] private Image imgThumbnail;
        [SerializeField] private RectTransform textEquipped;
        [SerializeField] private RectTransform multipleSelectedLayout;
        [SerializeField] private RectTransform imgHighlighted;
        [SerializeField] private RectTransform imgFocused;
        [SerializeField] private RectTransform imgAlertDot;
        [SerializeField] private TextMeshProUGUI textNumber;

        [Title("背景/边框配置")] [SerializeField] private Color weaponBackgroundColor;
        [SerializeField] private Sprite weaponBorder;
        [SerializeField] private Color gearBackgroundColor;
        [SerializeField] private Sprite gearBorder;
        [SerializeField] private Color itemBackgroundColor;
        [SerializeField] private Sprite itemBorder;
        [SerializeField] private Color materialBackgroundColor;
        [SerializeField] private Sprite materialBorder;

        private RecyclerViewHolderSelectable _selectable;
        private PackageManager _packageManager;
        private GameScene _gameScene;
        
        public GamePackageUIData Data { private set; get; }
        private System.Action<RecyclerViewHolder, MoveDirection> _navigationMoveCallback;

        public void Init(PackageManager packageManager, GameScene gameScene)
        {
            _selectable = GetComponent<RecyclerViewHolderSelectable>();
            _selectable.OnSelectionStateNormal += OnStateNormal;
            _selectable.OnSelectionStateHighlighted += OnStateHighlighted;
            _selectable.OnSelectionStatePressed += OnStateHighlighted;
            _selectable.OnSelectionStateSelected += OnStateSelected;
            _selectable.OnNavigationSelect += OnSelect;
            _selectable.OnNavigationDeselect += OnDeselect;
            _selectable.OnNavigationMove += OnMove;
            _packageManager = packageManager;
            _gameScene = gameScene;
        }

        public void Bind(
            GamePackageUIData itemUIData,
            System.Action<RecyclerViewHolder, MoveDirection> navigationMoveCallback
        )
        {
            Data = itemUIData;
            _navigationMoveCallback = navigationMoveCallback;

            // 设置物品类型UI
            switch (itemUIData.PackageGroup.Data)
            {
                case PackageWeaponData weaponData:
                {
                    imgBackground.color = weaponBackgroundColor;
                    imgBorder.sprite = weaponBorder;
                    textEquipped.gameObject.SetActive(_packageManager.IsGroupEquipped(itemUIData.PackageGroup.GroupId));
                }
                    break;
                case PackageGearData gearData:
                {
                    imgBackground.color = gearBackgroundColor;
                    imgBorder.sprite = gearBorder;
                    textEquipped.gameObject.SetActive(_packageManager.IsGroupEquipped(itemUIData.PackageGroup.GroupId));
                }
                    break;
                case PackageItemData itemData:
                {
                    imgBackground.color = itemBackgroundColor;
                    imgBorder.sprite = itemBorder;
                    textEquipped.gameObject.SetActive(false);
                }
                    break;
                case PackageMaterialData materialData:
                {
                    imgBackground.color = materialBackgroundColor;
                    imgBorder.sprite = materialBorder;
                    textEquipped.gameObject.SetActive(false);
                }
                    break;
            }

            // 加载物品缩略图
            var data = Data;
            _gameScene.LoadAssetAsyncTemporary<SpriteAtlas>(itemUIData.PackageGroup.Data.ThumbnailAtlas,
                handle =>
                {
                    if (data != Data) return;
                    imgThumbnail.sprite = handle.GetSprite(itemUIData.PackageGroup.Data.ThumbnailName);
                });

            // 展示物品属性
            multipleSelectedLayout.gameObject.SetActive(itemUIData.MultipleSelected);
            imgAlertDot.gameObject.SetActive(itemUIData.PackageGroup.New);
            textNumber.text = itemUIData.PackageGroup.Number.ToString();
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