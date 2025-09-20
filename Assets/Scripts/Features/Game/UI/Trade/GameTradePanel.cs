using System.Linq;
using Buff;
using Common;
using Features.Game.Data;
using Features.Game.UI.Package;
using Framework.Common.Audio;
using Framework.Common.UI.Panel;
using Framework.Common.UI.RecyclerView;
using Framework.Common.UI.RecyclerView.LayoutManager;
using Framework.Common.UI.Toast;
using Framework.Common.Util;
using Humanoid.Data;
using Humanoid.Weapon;
using Inputs;
using Package;
using Package.Data;
using Package.Data.Extension;
using Package.Runtime;
using Player;
using Sirenix.OdinInspector;
using Skill;
using TMPro;
using Trade;
using Trade.Runtime;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using VContainer;
using PlayerInputManager = Inputs.PlayerInputManager;

namespace Features.Game.UI.Trade
{
    public class GameTradePanel : BaseUGUIPanel
    {
        [Title("列表配置")] [SerializeField] private RecyclerViewSelectable listSelectable;
        [SerializeField] private RecyclerViewLayoutManager layoutManager;
        [SerializeField] private GameTradeGoodsListAdapter adapter;

        [Title("弹窗配置")] [SerializeField] private GamePackageDetailPopup detailPopup;

        [Title("音效配置")] [SerializeField] private AudioClip tradeSuccessAudioClip;
        [SerializeField] private AudioClip tradeFailureAudioClip;

        [Inject] private PlayerInputManager _playerInputManager;
        [Inject] private InputInfoManager _inputInfoManager;
        [Inject] private SkillManager _skillManager;
        [Inject] private BuffManager _buffManager;
        [Inject] private PackageManager _packageManager;
        [Inject] private TradeManager _tradeManager;
        [Inject] private PlayerDataManager _playerDataManager;
        [Inject] private GameUIModel _gameUIModel;
        [Inject] private GameManager _gameManager;
        [Inject] private EventSystem _eventSystem;
        [Inject] private AudioManager _audioManager;
        [Inject] private HumanoidWeaponManager _weaponManager;
        [Inject] private GameScene _gameScene;

        #region 头部栏

        private Button _btnClose;
        private TextMeshProUGUI _textTitle;
        private TextMeshProUGUI _textResourceMoney;

        #endregion

        #region 标签栏

        private Image _imgTabAllNormal;
        private Image _imgTabAllSelected;
        private Image _imgTabWeaponNormal;
        private Image _imgTabWeaponSelected;
        private Image _imgTabGearNormal;
        private Image _imgTabGearSelected;
        private Image _imgTabItemNormal;
        private Image _imgTabItemSelected;
        private Image _imgTabMaterialNormal;
        private Image _imgTabMaterialSelected;

        #endregion

        #region 内容布局

        private TextMeshProUGUI _textEmpty;

        private RecyclerView _rvGoodsList;

        #endregion

        #region 底部栏

        private TextMeshProUGUI _textTotalMoney;
        private TextMeshProUGUI _textNavigate;
        private TextMeshProUGUI _textChangeNumber;
        private TextMeshProUGUI _textTrade;
        private TextMeshProUGUI _textSubmit;

        #endregion

        [Title("运行时数据")] [ShowInInspector, ReadOnly]
        private global::Trade.Runtime.Trade _trade;

        private GameTradeModel _tradeModel;

        private VectorToDirectionInterceptor _vectorToDirectionInterceptor;

        private VectorToDirectionInterceptor VectorToDirectionInterceptor
        {
            get
            {
                if (!_vectorToDirectionInterceptor)
                {
                    _vectorToDirectionInterceptor = gameObject.GetComponent<VectorToDirectionInterceptor>();
                    if (!_vectorToDirectionInterceptor)
                    {
                        _vectorToDirectionInterceptor = gameObject.AddComponent<VectorToDirectionInterceptor>();
                        _vectorToDirectionInterceptor.cooldownDuration = 0.2f;
                    }
                }

                return _vectorToDirectionInterceptor;
            }
        }

        private const int ContinuousPressedThreshold = 3;
        private int _continuousPressedCount = 0;
        private VectorToDirectionInterceptor.Direction _lastDirection;
        private bool ContinuousPressed => _continuousPressedCount >= ContinuousPressedThreshold;

        protected override void OnInit()
        {
            _btnClose = GetWidget<Button>("BtnClose");
            _textTitle = GetWidget<TextMeshProUGUI>("TextTitle");
            _textResourceMoney = GetWidget<TextMeshProUGUI>("TextResourceMoney");

            _imgTabAllNormal = GetWidget<Image>("ImgTabAllNormal");
            _imgTabAllSelected = GetWidget<Image>("ImgTabAllSelected");
            _imgTabWeaponNormal = GetWidget<Image>("ImgTabWeaponNormal");
            _imgTabWeaponSelected = GetWidget<Image>("ImgTabWeaponSelected");
            _imgTabGearNormal = GetWidget<Image>("ImgTabGearNormal");
            _imgTabGearSelected = GetWidget<Image>("ImgTabGearSelected");
            _imgTabItemNormal = GetWidget<Image>("ImgTabItemNormal");
            _imgTabItemSelected = GetWidget<Image>("ImgTabItemSelected");
            _imgTabMaterialNormal = GetWidget<Image>("ImgTabMaterialNormal");
            _imgTabMaterialSelected = GetWidget<Image>("ImgTabMaterialSelected");

            _textEmpty = GetWidget<TextMeshProUGUI>("TextEmpty");

            _rvGoodsList = GetWidget<RecyclerView>("RvGoodsList");
            _rvGoodsList.Init();
            _rvGoodsList.LayoutManager = layoutManager;
            adapter.Init(HandleDecreaseTargetNumber, HandleIncreaseTargetNumber, HandleNavigationMove);
            _rvGoodsList.Adapter = adapter;

            _textTotalMoney = GetWidget<TextMeshProUGUI>("TextTotalMoney");
            _textNavigate = GetWidget<TextMeshProUGUI>("TextNavigate");
            _textChangeNumber = GetWidget<TextMeshProUGUI>("TextChangeNumber");
            _textTrade = GetWidget<TextMeshProUGUI>("TextTrade");
            _textSubmit = GetWidget<TextMeshProUGUI>("TextSubmit");

            detailPopup.AnchoredPositionGetter = rectTransform =>
            {
                var corners = new Vector3[4];
                rectTransform.GetWorldCorners(corners);
                return new Vector2(corners[2].x, (corners[2].y + corners[3].y) / 2);
            };
            detailPopup.PopupPositionSetter = ((anchoredPosition, rectTransform) =>
            {
                var screenSize = new Vector2(Screen.width, Screen.height);
                var popupSize = rectTransform.sizeDelta;
                var left = anchoredPosition.x - popupSize.x;
                var right = screenSize.x - anchoredPosition.x - popupSize.x;
                var toRight = right >= 0 || right > left;
                var top = screenSize.y - anchoredPosition.y - popupSize.y;
                var bottom = anchoredPosition.y - popupSize.y;
                var toBottom = bottom >= 0 || bottom > top;
                if (toRight)
                {
                    rectTransform.pivot = toBottom ? new Vector2(0, 1) : new Vector2(0, 0);
                }
                else
                {
                    rectTransform.pivot = toBottom ? new Vector2(1, 1) : new Vector2(1, 0);
                }

                rectTransform.transform.position = anchoredPosition;
            });
        }

        protected override void OnShow(object payload)
        {
            detailPopup.Init(_skillManager, _buffManager, _packageManager, _gameScene);
            _trade = payload as global::Trade.Runtime.Trade;
            _tradeModel = new GameTradeModel(_trade, _tradeManager, _gameManager, _packageManager, _weaponManager);

            // 监听返回按钮点击事件
            _btnClose.onClick.AddListener(HandleButtonCloseClicked);

            // 监听玩家输入
            _playerInputManager.RegisterActionPerformed(InputConstants.Previous, HandlePreviousPerformed);
            _playerInputManager.RegisterActionPerformed(InputConstants.Next, HandleNextPerformed);
            _playerInputManager.RegisterActionPerformed(InputConstants.Delete, HandleSwitchPerformed);
            _playerInputManager.RegisterActionPerformed(InputConstants.Submit, HandleConfirmPerformed);

            // 监听页面数据变化
            _tradeModel.GetMode().ObserveForever(HandleModeChanged);
            _tradeModel.GetTab().ObserveForever(HandleTabChanged);
            _tradeModel.GetPage().ObserveForever(HandlePageChanged);

            _tradeModel.SwitchMode(GameTradeMode.Payment);
            _tradeModel.SwitchTab(GameTradeTab.All);

            VectorToDirectionInterceptor.Refresh();
            _continuousPressedCount = 0;
            _lastDirection = VectorToDirectionInterceptor.Direction.Up;

            _rvGoodsList.ScrollToPosition(0);
            if (Focus)
            {
                _eventSystem.SetSelectedGameObject(null);
            }
        }

        protected override void OnShowingUpdate(bool focus)
        {
            UpdateUIParameters();
            UpdateDetailPopup();
            // 如果未满足按压状态则重置连续按压次数，否则处理按压
            var inputAction = _playerInputManager.GetInputAction(InputConstants.Navigate);
            if (!focus || !inputAction.IsPressed())
            {
                _continuousPressedCount = 0;
            }
            else
            {
                HandleNavigatePressed(inputAction.ReadValue<Vector2>());
            }

            // 如果未选中页面元素则在导航时设置默认选中
            if (focus && !_eventSystem.currentSelectedGameObject &&
                _playerInputManager.WasPerformedThisFrame(InputConstants.Navigate))
            {
                _eventSystem.SetSelectedGameObject(_btnClose.gameObject);
            }

            // 按下取消键则选中关闭按钮
            if (focus && _playerInputManager.WasPerformedThisFrame(InputConstants.Cancel))
            {
                _eventSystem.SetSelectedGameObject(_btnClose.gameObject);
            }
        }

        protected override void OnHide()
        {
            // 取消监听返回按钮点击事件
            _btnClose.onClick.AddListener(HandleButtonCloseClicked);

            // 取消监听玩家输入
            _playerInputManager.UnregisterActionPerformed(InputConstants.Previous, HandlePreviousPerformed);
            _playerInputManager.UnregisterActionPerformed(InputConstants.Next, HandleNextPerformed);
            _playerInputManager.UnregisterActionPerformed(InputConstants.Delete, HandleSwitchPerformed);
            _playerInputManager.UnregisterActionPerformed(InputConstants.Submit, HandleConfirmPerformed);

            // 取消监听页面数据变化
            _tradeModel.GetMode().RemoveObserver(HandleModeChanged);
            _tradeModel.GetTab().RemoveObserver(HandleTabChanged);
            _tradeModel.GetPage().RemoveObserver(HandlePageChanged);

            _tradeModel.Destroy();
            _tradeModel = null;

            UGUIUtil.DeselectIfSelectedInTargetChildren(_eventSystem, transform);
        }

        private void UpdateUIParameters()
        {
            _textResourceMoney.text = _playerDataManager.Money.ToString();

            if (_eventSystem.currentSelectedGameObject == _btnClose.gameObject)
            {
                _textSubmit.gameObject.SetActive(true);
                _textChangeNumber.gameObject.SetActive(false);
                _textTrade.gameObject.SetActive(false);
            }
            else
            {
                _textSubmit.gameObject.SetActive(false);
                _textChangeNumber.gameObject.SetActive(true);
                _textTrade.gameObject.SetActive(true);
            }

            // 计算所选商品的总价格
            if (_tradeModel.GetPage().HasValue())
            {
                _textTotalMoney.text = _tradeModel.GetPage().Value.Goods
                    .Where(data => data.Available && data.TargetNumber > 0)
                    .Sum(data => data.TotalMoney).ToString();
            }
            else
            {
                _textTotalMoney.text = "0";
            }
        }

        private void UpdateDetailPopup()
        {
            var itemViewHolder =
                _eventSystem.currentSelectedGameObject?.GetComponent<GameTradeGoodsItemViewHolder>();
            if (!itemViewHolder || itemViewHolder.Position == RecyclerView.NoPosition || _rvGoodsList.Scrolling)
            {
                detailPopup.Hide();
                return;
            }

            // 展示物品信息
            detailPopup.Show(
                itemViewHolder.RectTransform,
                PackageGroup.CreateNew(itemViewHolder.Data.PackageData, 0)
            );
        }

        private void HandleDecreaseTargetNumber(RecyclerViewHolder viewHolder)
        {
            if (_tradeModel.DecreaseGoodsNumber(viewHolder.Position, ContinuousPressed ? 10 : 1, out var data))
            {
                adapter.RefreshItem(viewHolder.Position, data);
            }
        }

        private void HandleIncreaseTargetNumber(RecyclerViewHolder viewHolder)
        {
            if (_tradeModel.IncreaseGoodsNumber(viewHolder.Position, ContinuousPressed ? 10 : 1, out var data))
            {
                adapter.RefreshItem(viewHolder.Position, data);
            }
        }

        private void HandleNavigationMove(RecyclerViewHolder viewHolder, MoveDirection moveDirection)
        {
            switch (moveDirection)
            {
                case MoveDirection.Up:
                {
                    if (_tradeModel.FocusUpperGoods(viewHolder.Position, out var index, out var data))
                    {
                        adapter.RefreshItem(index, data);
                        _rvGoodsList.FocusItem(index, true);
                    }
                    else
                    {
                        _eventSystem.SetSelectedGameObject(listSelectable.navigation.selectOnUp?.gameObject);
                    }
                }
                    break;
                case MoveDirection.Down:
                {
                    if (_tradeModel.FocusLowerGoods(viewHolder.Position, out var index, out var data))
                    {
                        adapter.RefreshItem(index, data);
                        _rvGoodsList.FocusItem(index, true);
                    }
                }
                    break;
            }
        }

        private void HandleButtonCloseClicked()
        {
            _tradeManager.FinishTrade(_trade.SerialNumber);
        }

        private void HandlePreviousPerformed(InputAction.CallbackContext obj)
        {
            if (!Focus)
            {
                return;
            }

            _tradeModel.SwitchToPreviousTab();
        }

        private void HandleNextPerformed(InputAction.CallbackContext obj)
        {
            if (!Focus)
            {
                return;
            }

            _tradeModel.SwitchToNextTab();
        }

        private void HandleSwitchPerformed(InputAction.CallbackContext obj)
        {
            if (!Focus)
            {
                return;
            }

            _tradeModel.SwitchMode(_tradeModel.GetMode().Value == GameTradeMode.Payment
                ? GameTradeMode.Sale
                : GameTradeMode.Payment);
        }

        private void HandleConfirmPerformed(InputAction.CallbackContext obj)
        {
            if (!Focus || !_eventSystem.currentSelectedGameObject)
            {
                return;
            }

            // 如果当前选中的是关闭按钮，则不执行后续
            if (_eventSystem.currentSelectedGameObject == _btnClose.gameObject)
            {
                return;
            }

            // 执行交易
            ExecuteTrade();
            return;

            void ExecuteTrade()
            {
                if (!_tradeModel.GetPage().HasValue())
                {
                    return;
                }

                var tradePage = _tradeModel.GetPage().Value;
                // 从商品列表中获取有效且目标数量大于0的选择商品
                var goods = tradePage.Goods
                    .Where(data => data.Available && data.TargetNumber > 0)
                    .ToList();

                // 如果商品列表为空，则不执行后续
                if (goods.Count == 0)
                {
                    return;
                }

                // 创建双方的订单
                var toSellOrder = new TradeOrder
                {
                    TradeSerialNumber = _trade.SerialNumber,
                    Items = goods.Select(item => new TradeOrderItem
                    {
                        SlotIndex = item.SellerSlotIndex,
                        PackageId = item.PackageData.Id,
                        Number = item.TargetNumber,
                        Money = item.TotalMoney,
                    }).ToList(),
                    TotalMoney = goods.Sum(item => item.TotalMoney),
                };
                var toPayOrder = new TradeOrder
                {
                    TradeSerialNumber = _trade.SerialNumber,
                    Items = goods.Select(item => new TradeOrderItem
                    {
                        SlotIndex = item.PayerSlotIndex,
                        PackageId = item.PackageData.Id,
                        Number = item.TargetNumber,
                        Money = item.TotalMoney,
                    }).ToList(),
                    TotalMoney = goods.Sum(item => item.TotalMoney),
                };

                #region 双方检测预售和预付订单

                if (!_tradeManager.Presell(tradePage.Seller, toSellOrder, out var presellFailureReason))
                {
                    switch (presellFailureReason)
                    {
                        case TradePresellFailureReason.TradeNotExist:
                        {
                            _audioManager.PlaySound(tradeFailureAudioClip);
                            switch (_tradeModel.GetMode().Value)
                            {
                                case GameTradeMode.Payment:
                                {
                                    Toast.Instance.Show("无法购买非法商品");
                                    return;
                                }
                                case GameTradeMode.Sale:
                                {
                                    Toast.Instance.Show("无法出售非法商品");
                                    return;
                                }
                            }
                        }
                            break;
                        case TradePresellFailureReason.CantSell:
                        {
                            _audioManager.PlaySound(tradeFailureAudioClip);
                            switch (_tradeModel.GetMode().Value)
                            {
                                case GameTradeMode.Payment:
                                {
                                    Toast.Instance.Show("无法购买非法商品");
                                    return;
                                }
                                case GameTradeMode.Sale:
                                {
                                    Toast.Instance.Show("无法出售非法商品");
                                    return;
                                }
                            }
                        }
                            break;
                        case TradePresellFailureReason.UnderStock:
                        {
                            _audioManager.PlaySound(tradeFailureAudioClip);
                            switch (_tradeModel.GetMode().Value)
                            {
                                case GameTradeMode.Payment:
                                {
                                    Toast.Instance.Show("商品库存不足，无法购买");
                                    return;
                                }
                                case GameTradeMode.Sale:
                                {
                                    Toast.Instance.Show("商品库存不足，无法出售");
                                    return;
                                }
                            }
                        }
                            break;
                    }

                    return;
                }

                if (!_tradeManager.Prepay(tradePage.Payer, toPayOrder, out var prepayFailureReason))
                {
                    switch (prepayFailureReason)
                    {
                        case TradePrepayFailureReason.TradeNotExist:
                        {
                            _audioManager.PlaySound(tradeFailureAudioClip);
                            switch (_tradeModel.GetMode().Value)
                            {
                                case GameTradeMode.Payment:
                                {
                                    Toast.Instance.Show("无法购买非法商品");
                                    return;
                                }
                                case GameTradeMode.Sale:
                                {
                                    Toast.Instance.Show("无法出售非法商品");
                                    return;
                                }
                            }
                        }
                            break;
                        case TradePrepayFailureReason.CantPay:
                        {
                            _audioManager.PlaySound(tradeFailureAudioClip);
                            switch (_tradeModel.GetMode().Value)
                            {
                                case GameTradeMode.Payment:
                                {
                                    Toast.Instance.Show("无法购买非法商品");
                                    return;
                                }
                                case GameTradeMode.Sale:
                                {
                                    Toast.Instance.Show("无法出售非法商品");
                                    return;
                                }
                            }
                        }
                            break;
                        case TradePrepayFailureReason.ExceedDemand:
                        {
                            _audioManager.PlaySound(tradeFailureAudioClip);
                            switch (_tradeModel.GetMode().Value)
                            {
                                case GameTradeMode.Payment:
                                {
                                    Toast.Instance.Show("商品数量超出预期，无法购买");
                                    return;
                                }
                                case GameTradeMode.Sale:
                                {
                                    Toast.Instance.Show("商品数量超出预期，无法出售");
                                    return;
                                }
                            }
                        }
                            break;
                        case TradePrepayFailureReason.MoneyNotEnough:
                        {
                            _audioManager.PlaySound(tradeFailureAudioClip);
                            switch (_tradeModel.GetMode().Value)
                            {
                                case GameTradeMode.Payment:
                                {
                                    Toast.Instance.Show("玩家金币不足，无法购买");
                                    return;
                                }
                                case GameTradeMode.Sale:
                                {
                                    Toast.Instance.Show("对方金币不足，无法出售");
                                    return;
                                }
                            }

                            break;
                        }
                    }

                    return;
                }

                #endregion

                // 双方执行出售和购买订单
                _tradeManager.Sell(tradePage.Seller, toSellOrder);
                _tradeManager.Pay(tradePage.Payer, toPayOrder);
                _audioManager.PlaySound(tradeSuccessAudioClip);
            }
        }

        private void HandleModeChanged(GameTradeMode mode)
        {
            switch (mode)
            {
                case GameTradeMode.Payment:
                    _textTitle.text = "购买页";
                    break;
                case GameTradeMode.Sale:
                    _textTitle.text = "出售页";
                    break;
            }
        }

        private void HandleTabChanged(GameTradeTab tab)
        {
            _imgTabAllSelected.gameObject.SetActive(false);
            _imgTabWeaponSelected.gameObject.SetActive(false);
            _imgTabGearSelected.gameObject.SetActive(false);
            _imgTabItemSelected.gameObject.SetActive(false);
            _imgTabMaterialSelected.gameObject.SetActive(false);
            _imgTabAllNormal.gameObject.SetActive(true);
            _imgTabWeaponNormal.gameObject.SetActive(true);
            _imgTabGearNormal.gameObject.SetActive(true);
            _imgTabItemNormal.gameObject.SetActive(true);
            _imgTabMaterialNormal.gameObject.SetActive(true);

            switch (tab)
            {
                case GameTradeTab.All:
                    _imgTabAllNormal.gameObject.SetActive(false);
                    _imgTabAllSelected.gameObject.SetActive(true);
                    break;
                case GameTradeTab.Weapon:
                    _imgTabWeaponNormal.gameObject.SetActive(false);
                    _imgTabWeaponSelected.gameObject.SetActive(true);
                    break;
                case GameTradeTab.Gear:
                    _imgTabGearNormal.gameObject.SetActive(false);
                    _imgTabGearSelected.gameObject.SetActive(true);
                    break;
                case GameTradeTab.Item:
                    _imgTabItemNormal.gameObject.SetActive(false);
                    _imgTabItemSelected.gameObject.SetActive(true);
                    break;
                case GameTradeTab.Material:
                    _imgTabMaterialNormal.gameObject.SetActive(false);
                    _imgTabMaterialSelected.gameObject.SetActive(true);
                    break;
            }
        }

        private void HandlePageChanged(GameTradePageUIData tradePageUIData)
        {
            _textEmpty.gameObject.SetActive(tradePageUIData.Goods.Count == 0);
            adapter.SetData(tradePageUIData.Goods);
        }

        private void HandleNavigatePressed(Vector2 navigation)
        {
            VectorToDirectionInterceptor.Intercept(navigation, direction =>
            {
                if (_lastDirection == direction)
                {
                    _continuousPressedCount++;
                }
                else
                {
                    _continuousPressedCount = 0;
                }

                _lastDirection = direction;

                if (!_eventSystem.currentSelectedGameObject || !_eventSystem.currentSelectedGameObject
                        .TryGetComponent<GameTradeGoodsItemViewHolder>(
                            out var itemViewHolder) || itemViewHolder.Position == RecyclerView.NoPosition)
                {
                    return;
                }

                switch (direction)
                {
                    case VectorToDirectionInterceptor.Direction.Left:
                    {
                        HandleDecreaseTargetNumber(itemViewHolder);
                    }
                        break;
                    case VectorToDirectionInterceptor.Direction.Right:
                    {
                        HandleIncreaseTargetNumber(itemViewHolder);
                    }
                        break;
                }
            });
        }
    }
}