using System;
using System.Collections;
using System.Collections.Generic;
using Buff;
using Common;
using Features.Game.Data;
using Features.Game.UI.Buff;
using Features.Game.UI.Package;
using Framework.Common.UI.Panel;
using Framework.Common.UI.RecyclerView;
using Framework.Common.UI.RecyclerView.LayoutManager;
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
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using VContainer;
using VContainer.Unity;
using PlayerInputManager = Inputs.PlayerInputManager;

namespace Features.Game.UI.Character
{
    public class GameCharacterPanel : BaseUGUIPanel
    {
        [Title("模型配置")] [SerializeField] private HumanoidCharacterObject modelCharacter;
        [SerializeField] private Transform showPoint;
        [SerializeField] private Animator modelAnimator;
        [SerializeField] private RuntimeAnimatorController maleIdleAnimatorController;
        [SerializeField] private RuntimeAnimatorController femaleIdleAnimatorController;

        [Title("列表配置")] [SerializeField] private RecyclerViewSelectable abilityListSelectable;
        [SerializeField] private RecyclerViewLayoutManager abilityListLayoutManager;
        [SerializeField] private GameCharacterSkillListAdapter abilityListAdapter;
        [SerializeField] private RecyclerViewSelectable magicListSelectable;
        [SerializeField] private RecyclerViewLayoutManager magicListLayoutManager;
        [SerializeField] private GameCharacterSkillListAdapter magicListAdapter;
        [SerializeField] private RecyclerViewSelectable buffListSelectable;
        [SerializeField] private RecyclerViewGridLayoutManager buffListLayoutManager;
        [SerializeField] private GameBuffDetailListAdapter buffListAdapter;

        [Title("弹窗配置")] [SerializeField] private GamePackageDetailPopup packageDetailPopup;
        [SerializeField] private GameCharacterSkillDetailPopup skillDetailPopup;
        [SerializeField] private GameBuffDetailPopup buffDetailPopup;

        #region 头部栏

        private Button _btnBack;

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

        #region 技能栏

        private RecyclerView _rvAbilitySkillList;
        private TextMeshProUGUI _textAbilitySkillEmpty;

        private RecyclerView _rvMagicSkillList;
        private TextMeshProUGUI _textMagicSkillEmpty;

        #endregion

        #region Buff栏

        private RecyclerView _rvBuffList;
        private TextMeshProUGUI _textBuffEmpty;

        #endregion

        private TextMeshProUGUI _textSubmit;

        [Inject] private PlayerInputManager _playerInputManager;
        [Inject] private GameUIModel _gameUIModel;
        [Inject] private GameManager _gameManager;
        [Inject] private PlayerDataManager _playerDataManager;
        [Inject] private SkillManager _skillManager;
        [Inject] private BuffManager _buffManager;
        [Inject] private PackageManager _packageManager;
        [Inject] private IObjectResolver _objectResolver;
        [Inject] private EventSystem _eventSystem;
        [Inject] private GameScene _gameScene;

        private PlayerCharacterObject Player => _gameManager.Player;

        private GameCharacterModel _characterModel;

        protected override void OnInit()
        {
            #region 关联UI

            _btnBack = GetWidget<Button>("BtnBack");

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

            _rvAbilitySkillList = GetWidget<RecyclerView>("RvAbilitySkillList");
            _rvAbilitySkillList.Init();
            _rvAbilitySkillList.LayoutManager = abilityListLayoutManager;
            abilityListAdapter.Init(HandleAbilityListNavigationMove);
            _rvAbilitySkillList.Adapter = abilityListAdapter;
            _textAbilitySkillEmpty = GetWidget<TextMeshProUGUI>("TextAbilitySkillEmpty");

            _rvMagicSkillList = GetWidget<RecyclerView>("RvMagicSkillList");
            _rvMagicSkillList.Init();
            _rvMagicSkillList.LayoutManager = magicListLayoutManager;
            magicListAdapter.Init(HandleMagicListNavigationMove);
            _rvMagicSkillList.Adapter = magicListAdapter;
            _textMagicSkillEmpty = GetWidget<TextMeshProUGUI>("TextMagicSkillEmpty");

            _rvBuffList = GetWidget<RecyclerView>("RvBuffList");
            _rvBuffList.Init();
            _rvBuffList.LayoutManager = buffListLayoutManager;
            buffListAdapter.Init(HandleBuffListNavigationMove);
            _rvBuffList.Adapter = buffListAdapter;
            _textBuffEmpty = GetWidget<TextMeshProUGUI>("TextBuffEmpty");

            _textSubmit = GetWidget<TextMeshProUGUI>("TextSubmit");

            #endregion

            // 设置弹窗位置
            packageDetailPopup.PopupPositionSetter = ((anchoredPosition, rectTransform) =>
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
            skillDetailPopup.PopupPositionSetter = ((anchoredPosition, rectTransform) =>
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
            buffDetailPopup.PopupPositionSetter = ((anchoredPosition, rectTransform) =>
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
            packageDetailPopup.Init(_skillManager, _buffManager, _packageManager, _gameScene);
            _characterModel = new GameCharacterModel(Player, buffListLayoutManager.GetSpanCount());

            // 监听按钮点击事件
            _btnBack.onClick.AddListener(HandleButtonBackClicked);

            // 监听页面数据
            _characterModel.GetAbilitySkillList().ObserveForever(HandleAbilitySkillListChanged);
            _characterModel.GetMagicSkillList().ObserveForever(HandleMagicSkillListChanged);
            _characterModel.GetBuffList().ObserveForever(HandleBuffListChanged);

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
            InitWeaponsAndGears();

            // 初始化角色信息，这里使用协程是因为UI使用LayoutGroup布局，需要等待一帧才能完成布局
            StartCoroutine(InitPlayerInformation());

            _rvAbilitySkillList.ScrollToPosition(0);
            _rvMagicSkillList.ScrollToPosition(0);
            _rvBuffList.ScrollToPosition(0);
            if (Focus)
            {
                _eventSystem.SetSelectedGameObject(null);
            }
        }

        protected override void OnShowingUpdate(bool focus)
        {
            UpdateUIParameters();
            UpdateTips();
            UpdatePackageDetailPopup();
            UpdateSkillDetailPopup();
            UpdateBuffDetailPopup();
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

            void UpdateTips()
            {
                _textSubmit.gameObject.SetActive(_eventSystem.currentSelectedGameObject == _btnBack.gameObject);
            }

            void UpdatePackageDetailPopup()
            {
                RectTransform focusedRectTransform = null;
                PackageGroup focusedPackageGroup = null;
                var gridView = _eventSystem.currentSelectedGameObject?.GetComponent<GamePackageGridView>();
                if (gridView)
                {
                    focusedRectTransform = gridView.GetComponent<RectTransform>();
                    focusedPackageGroup = gridView.Data;
                }

                // 未成功获取物品就隐藏弹窗
                if (!focusedRectTransform || focusedPackageGroup == null)
                {
                    packageDetailPopup.Hide();
                    return;
                }

                // 展示物品详情弹窗
                packageDetailPopup.Show(focusedRectTransform, focusedPackageGroup);
            }

            void UpdateSkillDetailPopup()
            {
                RectTransform focusedRectTransform = null;
                GameCharacterSkillUIData focusedSkillData = null;
                var itemViewHolder =
                    _eventSystem.currentSelectedGameObject?.GetComponent<GameCharacterSkillItemViewHolder>();
                if (itemViewHolder)
                {
                    focusedRectTransform = itemViewHolder.RectTransform;
                    focusedSkillData = itemViewHolder.Data;
                }

                // 未成功获取技能就隐藏弹窗
                if (!focusedRectTransform || focusedSkillData == null)
                {
                    skillDetailPopup.Hide();
                    return;
                }

                // 展示技能详情弹窗
                skillDetailPopup.Show(focusedRectTransform, focusedSkillData);
            }

            void UpdateBuffDetailPopup()
            {
                RectTransform focusedRectTransform = null;
                GameBuffDetailUIData focusedBuffData = null;
                var itemViewHolder =
                    _eventSystem.currentSelectedGameObject?.GetComponent<GameBuffDetailItemViewHolder>();
                if (itemViewHolder)
                {
                    focusedRectTransform = itemViewHolder.RectTransform;
                    focusedBuffData = itemViewHolder.Data;
                }

                // 未成功获取Buff就隐藏弹窗
                if (!focusedRectTransform || focusedBuffData == null)
                {
                    buffDetailPopup.Hide();
                    return;
                }

                // 展示Buff详情弹窗
                buffDetailPopup.Show(focusedRectTransform, focusedBuffData);
            }

            void UpdateModel()
            {
                modelCharacter.transform.position = showPoint.position;

                if (modelCharacter.WeaponAbility.LeftHandWeaponSlot != null)
                {
                    modelCharacter.WeaponAbility.LeftHandWeaponSlot.Object.gameObject.layer =
                        LayerMask.NameToLayer("UI");
                    modelCharacter.WeaponAbility.LeftHandWeaponSlot.Object.gameObject.transform.localScale =
                        100 * Vector3.one;
                }

                if (modelCharacter.WeaponAbility.RightHandWeaponSlot != null)
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
            // 取消监听按钮点击事件
            _btnBack.onClick.RemoveListener(HandleButtonBackClicked);

            // 取消监听页面数据
            _characterModel.GetAbilitySkillList().RemoveObserver(HandleAbilitySkillListChanged);
            _characterModel.GetMagicSkillList().RemoveObserver(HandleMagicSkillListChanged);
            _characterModel.GetBuffList().RemoveObserver(HandleBuffListChanged);

            UGUIUtil.DeselectIfSelectedInTargetChildren(_eventSystem, transform);
        }

        private void HandleAbilityListNavigationMove(RecyclerViewHolder viewHolder, MoveDirection moveDirection)
        {
            switch (moveDirection)
            {
                case MoveDirection.Up:
                {
                    if (_characterModel.FocusUpperAbilitySkill(viewHolder.Position, out var index, out var data))
                    {
                        abilityListAdapter.RefreshItem(index, data);
                        _rvAbilitySkillList.FocusItem(index, true);
                    }
                    else
                    {
                        _eventSystem.SetSelectedGameObject(abilityListSelectable.navigation.selectOnUp
                            ?.gameObject);
                    }
                }
                    break;
                case MoveDirection.Left:
                {
                    _eventSystem.SetSelectedGameObject(abilityListSelectable.navigation.selectOnLeft
                        ?.gameObject);
                }
                    break;
                case MoveDirection.Right:
                {
                    _eventSystem.SetSelectedGameObject(abilityListSelectable.navigation.selectOnRight
                        ?.gameObject);
                }
                    break;
                case MoveDirection.Down:
                {
                    if (_characterModel.FocusLowerAbilitySkill(viewHolder.Position, out var index, out var data))
                    {
                        abilityListAdapter.RefreshItem(index, data);
                        _rvAbilitySkillList.FocusItem(index, true);
                    }
                    else
                    {
                        _eventSystem.SetSelectedGameObject(abilityListSelectable.navigation.selectOnDown
                            ?.gameObject);
                    }
                }
                    break;
            }
        }

        private void HandleMagicListNavigationMove(RecyclerViewHolder viewHolder, MoveDirection moveDirection)
        {
            switch (moveDirection)
            {
                case MoveDirection.Up:
                {
                    if (_characterModel.FocusUpperMagicSkill(viewHolder.Position, out var index, out var data))
                    {
                        magicListAdapter.RefreshItem(index, data);
                        _rvMagicSkillList.FocusItem(index, true);
                    }
                    else
                    {
                        _eventSystem.SetSelectedGameObject(magicListSelectable.navigation.selectOnUp
                            ?.gameObject);
                    }
                }
                    break;
                case MoveDirection.Left:
                {
                    _eventSystem.SetSelectedGameObject(magicListSelectable.navigation.selectOnLeft
                        ?.gameObject);
                }
                    break;
                case MoveDirection.Down:
                {
                    if (_characterModel.FocusLowerMagicSkill(viewHolder.Position, out var index, out var data))
                    {
                        magicListAdapter.RefreshItem(index, data);
                        _rvMagicSkillList.FocusItem(index, true);
                    }
                    else
                    {
                        _eventSystem.SetSelectedGameObject(magicListSelectable.navigation.selectOnDown
                            ?.gameObject);
                    }
                }
                    break;
            }
        }

        private void HandleBuffListNavigationMove(RecyclerViewHolder viewHolder, MoveDirection moveDirection)
        {
            switch (moveDirection)
            {
                case MoveDirection.Up:
                {
                    if (_characterModel.FocusUpperBuff(viewHolder.Position, out var index, out var data))
                    {
                        buffListAdapter.RefreshItem(index, data);
                        _rvBuffList.FocusItem(index, true);
                    }
                    else
                    {
                        // 优先跳转到能力列表
                        if (_characterModel.GetAbilitySkillList().HasValue() &&
                            _characterModel.GetAbilitySkillList().Value.Count > 0)
                        {
                            _eventSystem.SetSelectedGameObject(_rvAbilitySkillList.gameObject);
                            break;
                        }

                        // 接着考虑魔法列表
                        if (_characterModel.GetMagicSkillList().HasValue() &&
                            _characterModel.GetMagicSkillList().Value.Count > 0)
                        {
                            _eventSystem.SetSelectedGameObject(_rvMagicSkillList.gameObject);
                            break;
                        }

                        // 最后才是预设配置物体
                        _eventSystem.SetSelectedGameObject(buffListSelectable.navigation.selectOnUp
                            ?.gameObject);
                    }
                }
                    break;
                case MoveDirection.Left:
                {
                    if (_characterModel.FocusLeftBuff(viewHolder.Position, out var index, out var data))
                    {
                        buffListAdapter.RefreshItem(index, data);
                        _rvBuffList.FocusItem(index, true);
                    }
                    else
                    {
                        _eventSystem.SetSelectedGameObject(buffListSelectable.navigation.selectOnLeft
                            ?.gameObject);
                    }
                }
                    break;
                case MoveDirection.Right:
                {
                    if (_characterModel.FocusRightBuff(viewHolder.Position, out var index, out var data))
                    {
                        buffListAdapter.RefreshItem(index, data);
                        _rvBuffList.FocusItem(index, true);
                    }
                }
                    break;
                case MoveDirection.Down:
                {
                    if (_characterModel.FocusLowerBuff(viewHolder.Position, out var index, out var data))
                    {
                        buffListAdapter.RefreshItem(index, data);
                        _rvBuffList.FocusItem(index, true);
                    }
                }
                    break;
            }
        }

        private void InitWeaponsAndGears()
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

        private IEnumerator InitPlayerInformation()
        {
            yield return new WaitForNextFrameUnit();
            _characterModel.FetchCharacterInformation();
        }

        private void HandleButtonBackClicked()
        {
            _gameUIModel.CharacterUI.SetValue(_gameUIModel.CharacterUI.Value.Close());
        }

        private void HandleAbilitySkillListChanged(List<GameCharacterSkillUIData> skills)
        {
            _textAbilitySkillEmpty.gameObject.SetActive(skills.Count == 0);
            abilityListAdapter.SetData(skills);
        }

        private void HandleMagicSkillListChanged(List<GameCharacterSkillUIData> skills)
        {
            _textMagicSkillEmpty.gameObject.SetActive(skills.Count == 0);
            magicListAdapter.SetData(skills);
        }

        private void HandleBuffListChanged(List<GameBuffDetailUIData> buffs)
        {
            _textBuffEmpty.gameObject.SetActive(buffs.Count == 0);
            buffListAdapter.SetData(buffs);
        }
    }
}