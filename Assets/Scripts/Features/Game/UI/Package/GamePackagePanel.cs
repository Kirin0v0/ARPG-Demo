using System.Collections.Generic;
using System.Linq;
using Buff;
using Buff.Data;
using Character;
using Character.Data;
using Common;
using Features.Game.Data;
using Framework.Common.Audio;
using Framework.Common.UI.Panel;
using Framework.Common.UI.RecyclerView;
using Framework.Common.UI.RecyclerView.LayoutManager;
using Framework.Common.UI.Toast;
using Framework.Common.Util;
using Humanoid;
using Inputs;
using Package;
using Package.Data;
using Package.Runtime;
using Player;
using Sirenix.OdinInspector;
using Skill;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using VContainer;
using VContainer.Unity;
using PlayerInputManager = Inputs.PlayerInputManager;

namespace Features.Game.UI.Package
{
    public class GamePackagePanel : BaseUGUIPanel
    {
        [Title("模型配置")] [SerializeField] private HumanoidCharacterObject modelCharacter;
        [SerializeField] private Transform showPoint;
        [SerializeField] private Animator modelAnimator;
        [SerializeField] private RuntimeAnimatorController maleIdleAnimatorController;
        [SerializeField] private RuntimeAnimatorController femaleIdleAnimatorController;

        [Title("列表配置")] [SerializeField] private RecyclerViewSelectable recyclerViewSelectable;
        [SerializeField] private RecyclerViewGridLayoutManager gridLayoutManager;
        [SerializeField] private GamePackageGridListAdapter adapter;

        [Title("弹窗配置")] [SerializeField] private GamePackageDetailPopup detailPopup;

        [Title("音效配置")] [SerializeField] private AudioClip weaponEquipAudioClip;
        [SerializeField] private AudioClip weaponUnequipAudioClip;
        [SerializeField] private AudioClip gearEquipAudioClip;
        [SerializeField] private AudioClip gearUnequipAudioClip;
        [SerializeField] private AudioClip itemUseAudioClip;
        [SerializeField] private AudioClip materialUseAudioClip;
        [SerializeField] private AudioClip deleteAudioClip;
        [SerializeField] private AudioClip operationErrorAudioClip;

        [Inject] private PlayerInputManager _playerInputManager;
        [Inject] private GameUIModel _gameUIModel;
        [Inject] private GameManager _gameManager;
        [Inject] private PlayerDataManager _playerDataManager;
        [Inject] private SkillManager _skillManager;
        [Inject] private BuffManager _buffManager;
        [Inject] private PackageManager _packageManager;
        [Inject] private IObjectResolver _objectResolver;
        [Inject] private EventSystem _eventSystem;
        [Inject] private AudioManager _audioManager;
        [Inject] private GameScene _gameScene;

        #region 头部栏

        private Button _btnBack;
        private TextMeshProUGUI _textResourceMoney;

        #endregion

        #region 装备栏和角色栏

        private GamePackageGridView _headGrid;
        private GamePackageGridView _torsoGrid;
        private GamePackageGridView _leftArmGrid;
        private GamePackageGridView _rightArmGrid;
        private GamePackageGridView _leftHandWeaponGrid;
        private GamePackageGridView _rightHandWeaponGrid;
        private GamePackageGridView _leftLegGrid;
        private GamePackageGridView _rightLegGrid;

        private TextMeshProUGUI _textCharacterName;
        private TextMeshProUGUI _textLevelNumber;
        private Slider _sliderLevelExperience;
        private TextMeshProUGUI _textLevelExperience;

        private Slider _sliderCharacterHp;
        private TextMeshProUGUI _textCharacterHpValue;
        private Slider _sliderCharacterMp;
        private TextMeshProUGUI _textCharacterMpValue;
        private TextMeshProUGUI _textCharacterPhysicsAttackValue;
        private TextMeshProUGUI _textCharacterMagicAttackValue;
        private TextMeshProUGUI _textCharacterDefenceValue;
        private TextMeshProUGUI _textCharacterStaminaValue;
        private TextMeshProUGUI _textCharacterStrengthValue;
        private TextMeshProUGUI _textCharacterMagicValue;
        private TextMeshProUGUI _textCharacterReactionValue;
        private TextMeshProUGUI _textCharacterLuckValue;

        #endregion

        #region 背包栏布局

        private RectTransform _imgTabAllNormal;
        private RectTransform _imgTabAllSelected;
        private Image _imgTabAllAlertDot;
        private RectTransform _imgTabWeaponNormal;
        private RectTransform _imgTabWeaponSelected;
        private Image _imgTabWeaponAlertDot;
        private RectTransform _imgTabGearNormal;
        private RectTransform _imgTabGearSelected;
        private Image _imgTabGearAlertDot;
        private RectTransform _imgTabItemNormal;
        private RectTransform _imgTabItemSelected;
        private Image _imgTabItemAlertDot;
        private RectTransform _imgTabMaterialNormal;
        private RectTransform _imgTabMaterialSelected;
        private Image _imgTabMaterialAlertDot;

        private RecyclerView _rvPackageList;

        private TextMeshProUGUI _textCountValue;

        #endregion

        private PlayerCharacterObject Player => _gameManager.Player;

        private GamePackageModel _packageModel;

        protected override void OnInit()
        {
            _btnBack = GetWidget<Button>("BtnBack");
            _textResourceMoney = GetWidget<TextMeshProUGUI>("TextResourceMoney");

            _headGrid = GetWidget<Button>("HeadGrid").GetComponent<GamePackageGridView>();
            _torsoGrid = GetWidget<Button>("TorsoGrid").GetComponent<GamePackageGridView>();
            _leftArmGrid = GetWidget<Button>("LeftArmGrid").GetComponent<GamePackageGridView>();
            _rightArmGrid = GetWidget<Button>("RightArmGrid").GetComponent<GamePackageGridView>();
            _leftHandWeaponGrid = GetWidget<Button>("LeftHandWeaponGrid").GetComponent<GamePackageGridView>();
            _rightHandWeaponGrid = GetWidget<Button>("RightHandWeaponGrid").GetComponent<GamePackageGridView>();
            _leftLegGrid = GetWidget<Button>("LeftLegGrid").GetComponent<GamePackageGridView>();
            _rightLegGrid = GetWidget<Button>("RightLegGrid").GetComponent<GamePackageGridView>();

            _textCharacterName = GetWidget<TextMeshProUGUI>("TextCharacterName");
            _textLevelNumber = GetWidget<TextMeshProUGUI>("TextLevelNumber");
            _sliderLevelExperience = GetWidget<Slider>("SliderLevelExperience");
            _textLevelExperience = GetWidget<TextMeshProUGUI>("TextLevelExperience");

            _sliderCharacterHp = GetWidget<Slider>("SliderCharacterHp");
            _textCharacterHpValue = GetWidget<TextMeshProUGUI>("TextCharacterHpValue");
            _sliderCharacterMp = GetWidget<Slider>("SliderCharacterMp");
            _textCharacterMpValue = GetWidget<TextMeshProUGUI>("TextCharacterMpValue");
            _textCharacterPhysicsAttackValue = GetWidget<TextMeshProUGUI>("TextCharacterPhysicsAttackValue");
            _textCharacterMagicAttackValue = GetWidget<TextMeshProUGUI>("TextCharacterMagicAttackValue");
            _textCharacterDefenceValue = GetWidget<TextMeshProUGUI>("TextCharacterDefenceValue");
            _textCharacterStaminaValue = GetWidget<TextMeshProUGUI>("TextCharacterStaminaValue");
            _textCharacterStrengthValue = GetWidget<TextMeshProUGUI>("TextCharacterStrengthValue");
            _textCharacterMagicValue = GetWidget<TextMeshProUGUI>("TextCharacterMagicValue");
            _textCharacterReactionValue = GetWidget<TextMeshProUGUI>("TextCharacterReactionValue");
            _textCharacterLuckValue = GetWidget<TextMeshProUGUI>("TextCharacterLuckValue");

            _imgTabAllNormal = GetWidget<RectTransform>("ImgTabAllNormal");
            _imgTabAllSelected = GetWidget<RectTransform>("ImgTabAllSelected");
            _imgTabAllAlertDot = GetWidget<Image>("ImgTabAllAlertDot");
            _imgTabWeaponNormal = GetWidget<RectTransform>("ImgTabWeaponNormal");
            _imgTabWeaponSelected = GetWidget<RectTransform>("ImgTabWeaponSelected");
            _imgTabWeaponAlertDot = GetWidget<Image>("ImgTabWeaponAlertDot");
            _imgTabGearNormal = GetWidget<RectTransform>("ImgTabGearNormal");
            _imgTabGearSelected = GetWidget<RectTransform>("ImgTabGearSelected");
            _imgTabGearAlertDot = GetWidget<Image>("ImgTabGearAlertDot");
            _imgTabItemNormal = GetWidget<RectTransform>("ImgTabItemNormal");
            _imgTabItemSelected = GetWidget<RectTransform>("ImgTabItemSelected");
            _imgTabItemAlertDot = GetWidget<Image>("ImgTabItemAlertDot");
            _imgTabMaterialNormal = GetWidget<RectTransform>("ImgTabMaterialNormal");
            _imgTabMaterialSelected = GetWidget<RectTransform>("ImgTabMaterialSelected");
            _imgTabMaterialAlertDot = GetWidget<Image>("ImgTabMaterialAlertDot");

            _rvPackageList = GetWidget<RecyclerView>("RvPackageList");
            _rvPackageList.Init();
            _rvPackageList.LayoutManager = gridLayoutManager;
            adapter.Init(HandleNavigationMove);
            _rvPackageList.Adapter = adapter;

            _textCountValue = GetWidget<TextMeshProUGUI>("TextCountValue");

            // 设置外部元素=>列表的导航行为
            recyclerViewSelectable.FromUpSelectableTransfer = SelectFirstViewHolder;
            recyclerViewSelectable.FromLeftSelectableTransfer = SelectFirstViewHolder;
            recyclerViewSelectable.FromRightSelectableTransfer = SelectLastViewHolder;
            recyclerViewSelectable.FromDownSelectableTransfer = SelectLastViewHolder;
            recyclerViewSelectable.FromUnknownSelectableTransfer = SelectFirstViewHolder;

            // 设置详情弹窗位置行为
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

            return;

            GameObject SelectFirstViewHolder(GameObject from)
            {
                var viewHolder = _rvPackageList.RecyclerQuery.GetVisibleViewHolders()
                    .OrderBy(viewHolder => viewHolder.Position)
                    .FirstOrDefault();
                if (!viewHolder)
                {
                    return null;
                }

                if (_packageModel.MarkPackageSeen(viewHolder.Position, out var data))
                {
                    adapter.RefreshItem(viewHolder.Position, data);
                }

                return viewHolder.gameObject;
            }

            GameObject SelectLastViewHolder(GameObject from)
            {
                var viewHolder = _rvPackageList.RecyclerQuery.GetVisibleViewHolders()
                    .OrderByDescending(viewHolder => viewHolder.Position)
                    .FirstOrDefault();
                if (!viewHolder)
                {
                    return null;
                }

                if (_packageModel.MarkPackageSeen(viewHolder.Position, out var data))
                {
                    adapter.RefreshItem(viewHolder.Position, data);
                }

                return viewHolder.gameObject;
            }
        }

        protected override void OnShow(object payload)
        {
            // 设置注入
            adapter.PackageManager = _packageManager;
            adapter.ObjectResolver = _objectResolver;
            adapter.GameScene = _gameScene;
            detailPopup.Init(_skillManager, _buffManager, _packageManager, _gameScene);

            // 初始化Model
            _packageModel = new GamePackageModel(_packageManager, gridLayoutManager.GetSpanCount(), 8);

            // 监听玩家输入
            _playerInputManager.RegisterActionPerformed(InputConstants.Previous, HandlePreviousPerformed);
            _playerInputManager.RegisterActionPerformed(InputConstants.Next, HandleNextPerformed);
            _playerInputManager.RegisterActionPerformed(InputConstants.Submit, HandleUsePerformed);
            _playerInputManager.RegisterActionPerformed(InputConstants.Select, HandleSelectPerformed);
            _playerInputManager.RegisterActionPerformed(InputConstants.Delete, HandleDeletePerformed);

            // 监听背包管理器武器和装备事件
            _packageManager.OnWeaponOrGearChanged += HandleWeaponOrGearChanged;

            // 监听按钮点击事件
            _btnBack.onClick.AddListener(HandleButtonBackClicked);

            // 监听页面数据变化
            _packageModel.GetTabType().ObserveForever(HandleTabTypeChanged);
            _packageModel.GetNewTabFlags().ObserveForever(HandleNewTabFlagsChanged);
            _packageModel.GetTabPackageList().ObserveForever(HandleTabPackageListChanged);

            // 初始化模型角色
            _objectResolver.InjectGameObject(modelCharacter.gameObject);
            modelCharacter.gameObject.SetActive(true);
            modelCharacter.Init();
            modelCharacter.SetHumanoidCharacterParameters(Player.HumanoidParameters.race,
                new List<PackageGroup>(), new List<PackageGroup>());
            modelCharacter.AppearanceAbility?.SetAppearance(Player.HumanoidParameters.appearance);
            modelAnimator.runtimeAnimatorController =
                Player.HumanoidParameters.race == HumanoidCharacterRace.HumanFemale
                    ? femaleIdleAnimatorController
                    : maleIdleAnimatorController;
            modelAnimator.updateMode = AnimatorUpdateMode.UnscaledTime;

            // 初始化武器和装备模型
            InitWeaponAndGears();

            // 初始化Tab页
            _packageModel.SwitchTab(GamePackageTabType.All);

            _rvPackageList.ScrollToPosition(0);
            if (Focus)
            {
                _eventSystem.SetSelectedGameObject(null);
            }
        }

        protected override void OnShowingUpdate(bool focus)
        {
            // 每帧更新UI
            UpdateUIParameters();
            UpdateFocusedPackage();
            UpdateModel();

            // 如果未选中页面元素则在导航时设置默认选中
            if (focus && !_eventSystem.currentSelectedGameObject &&
                _playerInputManager.WasPerformedThisFrame(InputConstants.Navigate))
            {
                _eventSystem.SetSelectedGameObject(_btnBack.gameObject);
            }

            // 按下取消键则选中返回按钮
            if (focus && _playerInputManager.WasPerformedThisFrame(InputConstants.Cancel))
            {
                _eventSystem.SetSelectedGameObject(_btnBack.gameObject);
            }

            return;

            void UpdateUIParameters()
            {
                _textResourceMoney.text = _playerDataManager.Money.ToString();
                _textCharacterName.text = Player.Parameters.name;
                _textLevelNumber.text = _playerDataManager.Level.ToString();
                _textLevelExperience.text = $"{_playerDataManager.Experience}/{_playerDataManager.LevelUpExperience}";
                // DOTween.To(() => _sliderLevelExperience.value, x => _sliderLevelExperience.value = x,
                //     experiencePercentage, 0.5f);
                _sliderLevelExperience.value =
                    1f * _playerDataManager.Experience / _playerDataManager.LevelUpExperience;
                // DOTween.To(() => _sliderCharacterHp.value, x => _sliderCharacterHp.value = x,
                //     1f * Player.Parameters.resource.hp / Player.Parameters.property.maxHp, 0.5f);
                _sliderCharacterHp.value = 1f * Player.Parameters.resource.hp / Player.Parameters.property.maxHp;
                _textCharacterHpValue.text = $"{Player.Parameters.resource.hp}/{Player.Parameters.property.maxHp}";
                // DOTween.To(() => _sliderCharacterMp.value, x => _sliderCharacterMp.value = x,
                //     1f * Player.Parameters.resource.mp / Player.Parameters.property.maxMp, 0.5f);
                _sliderCharacterMp.value = 1f * Player.Parameters.resource.mp / Player.Parameters.property.maxMp;
                _textCharacterMpValue.text = $"{Player.Parameters.resource.mp}/{Player.Parameters.property.maxMp}";
                _textCharacterPhysicsAttackValue.text = Player.Parameters.physicsAttack.ToString();
                _textCharacterMagicAttackValue.text = Player.Parameters.magicAttack.ToString();
                _textCharacterDefenceValue.text = Player.Parameters.defence.ToString();
                _textCharacterStaminaValue.text = Player.Parameters.property.stamina.ToString();
                _textCharacterStrengthValue.text = Player.Parameters.property.strength.ToString();
                _textCharacterMagicValue.text = Player.Parameters.property.magic.ToString();
                _textCharacterReactionValue.text = Player.Parameters.property.reaction.ToString();
                _textCharacterLuckValue.text = Player.Parameters.property.luck.ToString();
            }

            void UpdateFocusedPackage()
            {
                RectTransform focusedRectTransform = null;
                PackageGroup focusedPackageGroup = null;
                var itemViewHolder =
                    _eventSystem.currentSelectedGameObject?.GetComponent<GamePackageGridItemViewHolder>();
                if (itemViewHolder && itemViewHolder.Position != RecyclerView.NoPosition)
                {
                    focusedRectTransform = itemViewHolder.RectTransform;
                    focusedPackageGroup = itemViewHolder.Data.PackageGroup;
                    // 标记为已读物品
                    if (_packageModel.MarkPackageSeen(itemViewHolder.Position, out var data))
                    {
                        adapter.RefreshItem(itemViewHolder.Position, data);
                    }
                }
                else
                {
                    var gridView = _eventSystem.currentSelectedGameObject?.GetComponent<GamePackageGridView>();
                    if (gridView)
                    {
                        focusedRectTransform = gridView.GetComponent<RectTransform>();
                        focusedPackageGroup = gridView.Data;
                    }
                }

                // 未成功获取物品就隐藏物品详情弹窗
                if (!focusedRectTransform || focusedPackageGroup == null)
                {
                    detailPopup.Hide();
                    return;
                }

                // 展示物品详情弹窗
                detailPopup.Show(focusedRectTransform, focusedPackageGroup);
            }

            void UpdateModel()
            {
                modelCharacter.transform.position = showPoint.position;

                if (modelCharacter.WeaponAbility?.LeftHandWeaponSlot?.Object != null)
                {
                    modelCharacter.WeaponAbility.LeftHandWeaponSlot.Object.gameObject.layer =
                        LayerMask.NameToLayer("UI");
                    modelCharacter.WeaponAbility.LeftHandWeaponSlot.Object.gameObject.transform.localScale =
                        100 * Vector3.one;
                }

                if (modelCharacter.WeaponAbility?.RightHandWeaponSlot?.Object != null)
                {
                    modelCharacter.WeaponAbility.RightHandWeaponSlot.Object.gameObject.layer =
                        LayerMask.NameToLayer("UI");
                    modelCharacter.WeaponAbility.RightHandWeaponSlot.Object.gameObject.transform.localScale =
                        100 * Vector3.one;
                }
            }
        }

        protected override void OnHide()
        {
            // 取消监听玩家输入
            _playerInputManager.UnregisterActionPerformed(InputConstants.Previous, HandlePreviousPerformed);
            _playerInputManager.UnregisterActionPerformed(InputConstants.Next, HandleNextPerformed);
            _playerInputManager.UnregisterActionPerformed(InputConstants.Submit, HandleUsePerformed);
            _playerInputManager.UnregisterActionPerformed(InputConstants.Select, HandleSelectPerformed);
            _playerInputManager.UnregisterActionPerformed(InputConstants.Delete, HandleDeletePerformed);

            // 取消监听背包管理器武器和装备事件
            _packageManager.OnWeaponOrGearChanged -= HandleWeaponOrGearChanged;

            // 取消监听按钮点击事件
            _btnBack.onClick.RemoveListener(HandleButtonBackClicked);

            // 取消监听页面数据变化
            _packageModel.GetTabType().RemoveObserver(HandleTabTypeChanged);
            _packageModel.GetNewTabFlags().RemoveObserver(HandleNewTabFlagsChanged);
            _packageModel.GetTabPackageList().RemoveObserver(HandleTabPackageListChanged);

            // 销毁Model
            _packageModel.Destroy();
            _packageModel = null;

            UGUIUtil.DeselectIfSelectedInTargetChildren(_eventSystem, transform);
        }

        private void HandleNavigationMove(RecyclerViewHolder viewHolder, MoveDirection moveDirection)
        {
            switch (moveDirection)
            {
                case MoveDirection.Up:
                {
                    if (_packageModel.FocusUpperGrid(viewHolder.Position, out var index, out var data))
                    {
                        _packageModel.MarkPackageSeen(index, out var _);
                        adapter.RefreshItem(index, data);
                        _rvPackageList.FocusItem(index, true);
                    }
                    else
                    {
                        _eventSystem.SetSelectedGameObject(recyclerViewSelectable.navigation.selectOnUp
                            ?.gameObject);
                    }
                }
                    break;
                case MoveDirection.Left:
                {
                    if (_packageModel.FocusLeftGrid(viewHolder.Position, out var index, out var data))
                    {
                        _packageModel.MarkPackageSeen(index, out var _);
                        adapter.RefreshItem(index, data);
                        _rvPackageList.FocusItem(index, true);
                    }
                    else
                    {
                        _eventSystem.SetSelectedGameObject(recyclerViewSelectable.navigation.selectOnLeft
                            ?.gameObject);
                    }
                }
                    break;
                case MoveDirection.Right:
                {
                    if (_packageModel.FocusRightGrid(viewHolder.Position, out var index, out var data))
                    {
                        _packageModel.MarkPackageSeen(index, out var _);
                        adapter.RefreshItem(index, data);
                        _rvPackageList.FocusItem(index, true);
                    }
                }
                    break;
                case MoveDirection.Down:
                {
                    if (_packageModel.FocusLowerGrid(viewHolder.Position, out var index, out var data))
                    {
                        _packageModel.MarkPackageSeen(index, out var _);
                        adapter.RefreshItem(index, data);
                        _rvPackageList.FocusItem(index, true);
                    }
                }
                    break;
            }
        }

        private void HandlePreviousPerformed(InputAction.CallbackContext obj)
        {
            if (!Focus)
            {
                return;
            }

            _packageModel.SwitchToPreviousTab();
        }

        private void HandleNextPerformed(InputAction.CallbackContext obj)
        {
            if (!Focus)
            {
                return;
            }

            _packageModel.SwitchToNextTab();
        }

        private void HandleUsePerformed(InputAction.CallbackContext obj)
        {
            // 非焦点状态或未选中，不予处理
            if (!Focus)
            {
                return;
            }

            // 未选中物品子项ViewHolder或装备格，不予处理
            PackageGroup focusedPackageGroup = null;
            var itemViewHolder = _eventSystem.currentSelectedGameObject?.GetComponent<GamePackageGridItemViewHolder>();
            if (itemViewHolder && itemViewHolder.Position != RecyclerView.NoPosition)
            {
                focusedPackageGroup = itemViewHolder.Data.PackageGroup;
            }
            else
            {
                var gridView = _eventSystem.currentSelectedGameObject?.GetComponent<GamePackageGridView>();
                if (gridView)
                {
                    focusedPackageGroup = gridView.Data;
                }
            }

            if (focusedPackageGroup == null)
            {
                return;
            }

            // 与选中物品进行交互
            switch (focusedPackageGroup.Data)
            {
                case PackageWeaponData weaponData:
                {
                    if (Player.Parameters.battleState == CharacterBattleState.Battle)
                    {
                        _audioManager.PlaySound(operationErrorAudioClip);
                        Toast.Instance.Show("战斗中无法装上/卸下武器");
                        return;
                    }

                    if (_packageManager.IsGroupEquipped(focusedPackageGroup.GroupId))
                    {
                        _audioManager.PlaySound(weaponUnequipAudioClip);
                        Player.WeaponAbility.RemoveWeapon(focusedPackageGroup);
                    }
                    else
                    {
                        _audioManager.PlaySound(weaponEquipAudioClip);
                        Player.WeaponAbility.AddWeapon(focusedPackageGroup);
                    }
                }
                    break;
                case PackageGearData gearData:
                {
                    if (Player.Parameters.battleState == CharacterBattleState.Battle)
                    {
                        _audioManager.PlaySound(operationErrorAudioClip);
                        Toast.Instance.Show("战斗中无法穿上/卸下装备");
                        return;
                    }

                    if (_packageManager.IsGroupEquipped(focusedPackageGroup.GroupId))
                    {
                        if (!Player.EquipmentAbility.UnequipGear(focusedPackageGroup))
                        {
                            _audioManager.PlaySound(operationErrorAudioClip);
                            Toast.Instance.Show("无法卸下装备");
                        }
                        else
                        {
                            _audioManager.PlaySound(gearUnequipAudioClip);
                        }
                    }
                    else
                    {
                        if (!Player.EquipmentAbility.EquipGear(focusedPackageGroup))
                        {
                            _audioManager.PlaySound(operationErrorAudioClip);
                            Toast.Instance.Show("无法穿上装备");
                        }
                        else
                        {
                            _audioManager.PlaySound(gearEquipAudioClip);
                        }
                    }
                }
                    break;
                case PackageItemData itemData:
                {
                    _audioManager.PlaySound(itemUseAudioClip);
                    // 道具数量-1
                    _packageManager.DeletePackageGroup(focusedPackageGroup.GroupId, 1);
                    // 调整玩家资源
                    Player.ResourceAbility.Modify(new CharacterResource
                    {
                        hp = itemData.Hp,
                        mp = itemData.Mp,
                    });
                    // 赋予玩家Buff
                    if (_buffManager.TryGetBuffInfo(itemData.BuffId, out var buffInfo))
                    {
                        _buffManager.AddBuff(new BuffAddInfo
                        {
                            Info = buffInfo,
                            Caster = Player,
                            Target = Player,
                            Stack = itemData.BuffStack,
                            Permanent = false,
                            DurationType = BuffAddDurationType.SetDuration,
                            Duration = itemData.BuffDuration,
                        });
                    }

                    // 玩家习得能力
                    itemData.Skills.ForEach(skillId => { _playerDataManager.LearnAbilitySkill(skillId); });
                }
                    break;
                case PackageMaterialData materialData:
                {
                    _audioManager.PlaySound(operationErrorAudioClip);
                    Toast.Instance.Show("无法使用材料");
                }
                    break;
            }
        }

        private void HandleSelectPerformed(InputAction.CallbackContext obj)
        {
            // 非焦点状态，不予处理
            if (!Focus)
            {
                return;
            }

            // 未选中物品子项ViewHolder。不予处理
            var itemViewHolder = _eventSystem.currentSelectedGameObject?.GetComponent<GamePackageGridItemViewHolder>();
            if (!itemViewHolder || itemViewHolder.Position == RecyclerView.NoPosition)
            {
                return;
            }

            // 反转多选状态并刷新子项
            _packageModel.ToggleMultipleSelect(itemViewHolder.Data);
            adapter.NotifyItemChanged(itemViewHolder.Position);
        }

        private void HandleDeletePerformed(InputAction.CallbackContext obj)
        {
            if (!Focus)
            {
                return;
            }

            if (_packageModel.RemoveMultipleSelectedPackages(
                    successCallback: (index, packageGroup) => { },
                    failureCallback: (index, packageGroup) =>
                    {
                        Toast.Instance.Show($"无法删除{packageGroup.Data.Name}，请检查是否装备中");
                    }
                ))
            {
                _audioManager.PlaySound(deleteAudioClip);
            }
            else
            {
                _audioManager.PlaySound(operationErrorAudioClip);
            }
        }

        private void InitWeaponAndGears()
        {
            // 初始化装备栏
            _leftHandWeaponGrid.Init(_gameScene);
            _rightHandWeaponGrid.Init(_gameScene);
            _headGrid.Init(_gameScene);
            _torsoGrid.Init(_gameScene);
            _leftArmGrid.Init(_gameScene);
            _rightArmGrid.Init(_gameScene);
            _leftLegGrid.Init(_gameScene);
            _rightLegGrid.Init(_gameScene);
            // 设置装备栏数据
            HandleWeaponOrGearChanged();
        }

        private void HandleWeaponOrGearChanged()
        {
            // 设置装备栏数据
            _leftHandWeaponGrid.SetPackageGroup(_packageManager.LeftHandWeapon);
            _rightHandWeaponGrid.SetPackageGroup(_packageManager.RightHandWeapon);
            _headGrid.SetPackageGroup(_packageManager.HeadGear);
            _torsoGrid.SetPackageGroup(_packageManager.TorsoGear);
            _leftArmGrid.SetPackageGroup(_packageManager.LeftArmGear);
            _rightArmGrid.SetPackageGroup(_packageManager.RightArmGear);
            _leftLegGrid.SetPackageGroup(_packageManager.LeftLegGear);
            _rightLegGrid.SetPackageGroup(_packageManager.RightLegGear);

            // 更新玩家模型
            modelCharacter.AppearanceAbility.SetAppearance(Player.HumanoidParameters.appearance);
            modelCharacter.WeaponAbility.ClearWeaponBar();
            if (_packageManager.LeftHandWeapon != null)
            {
                modelCharacter.WeaponAbility.AddWeapon(_packageManager.LeftHandWeapon);
            }

            if (_packageManager.RightHandWeapon != null)
            {
                modelCharacter.WeaponAbility.AddWeapon(_packageManager.RightHandWeapon);
            }
        }

        private void HandleButtonBackClicked()
        {
            _gameUIModel.PackageUI.SetValue(_gameUIModel.PackageUI.Value.Close());
        }

        private void HandleTabTypeChanged(GamePackageTabType tabType)
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

            switch (tabType)
            {
                case GamePackageTabType.All:
                    _imgTabAllNormal.gameObject.SetActive(false);
                    _imgTabAllSelected.gameObject.SetActive(true);
                    break;
                case GamePackageTabType.Weapon:
                    _imgTabWeaponNormal.gameObject.SetActive(false);
                    _imgTabWeaponSelected.gameObject.SetActive(true);
                    break;
                case GamePackageTabType.Gear:
                    _imgTabGearNormal.gameObject.SetActive(false);
                    _imgTabGearSelected.gameObject.SetActive(true);
                    break;
                case GamePackageTabType.Item:
                    _imgTabItemNormal.gameObject.SetActive(false);
                    _imgTabItemSelected.gameObject.SetActive(true);
                    break;
                case GamePackageTabType.Material:
                    _imgTabMaterialNormal.gameObject.SetActive(false);
                    _imgTabMaterialSelected.gameObject.SetActive(true);
                    break;
            }
        }

        private void HandleNewTabFlagsChanged(GamePackageTabFlags newTabFlags)
        {
            _imgTabAllAlertDot.gameObject.SetActive(newTabFlags.HasFlag(GamePackageTabFlags.All));
            _imgTabWeaponAlertDot.gameObject.SetActive(newTabFlags.HasFlag(GamePackageTabFlags.Weapon));
            _imgTabGearAlertDot.gameObject.SetActive(newTabFlags.HasFlag(GamePackageTabFlags.Gear));
            _imgTabItemAlertDot.gameObject.SetActive(newTabFlags.HasFlag(GamePackageTabFlags.Item));
            _imgTabMaterialAlertDot.gameObject.SetActive(newTabFlags.HasFlag(GamePackageTabFlags.Material));
        }

        private void HandleTabPackageListChanged(List<object> packageList)
        {
            adapter.SetData(packageList);
            CalculatePackageItemCount();
        }

        private void CalculatePackageItemCount()
        {
            var list = _packageModel.GetTabPackageList().Value;
            var count = list.Count(item => item is GamePackageUIData);
            _textCountValue.text = $"{count}/{list.Count}";
        }
    }
}