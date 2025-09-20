using System;
using System.Collections.Generic;
using System.Linq;
using Archive;
using Archive.Data;
using Common;
using Events;
using Events.Data;
using Framework.Common.Debug;
using Framework.Common.Util;
using Humanoid;
using Humanoid.Data;
using Humanoid.Weapon;
using Humanoid.Weapon.SO;
using Package.Data;
using Package.Data.Extension;
using Package.Runtime;
using Player;
using Sirenix.OdinInspector;
using UnityEngine;
using VContainer;

namespace Package
{
    /// <summary>
    /// 物品管理器，特指管理玩家身上的物品，而不是全场物品，因此可视为背包管理器
    /// </summary>
    public class PackageManager : MonoBehaviour, IArchivable
    {
        private readonly List<PackageGroup> _packageGroups = new();
        public PackageGroup[] PackageGroups => _packageGroups.ToArray();

        [Title("运行时数据")]
        [ShowInInspector, ReadOnly] public PackageGroup LeftHandWeapon { private set; get; }
        [ShowInInspector, ReadOnly] public PackageGroup RightHandWeapon { private set; get; }

        public List<PackageGroup> Weapons
        {
            get
            {
                var weapons = new List<PackageGroup>();
                if (LeftHandWeapon != null)
                {
                    weapons.Add(LeftHandWeapon);
                }

                if (RightHandWeapon != null)
                {
                    weapons.Add(RightHandWeapon);
                }

                return weapons;
            }
        }

        [ShowInInspector, ReadOnly] public PackageGroup HeadGear { private set; get; }
        [ShowInInspector, ReadOnly] public PackageGroup TorsoGear { private set; get; }
        [ShowInInspector, ReadOnly] public PackageGroup LeftArmGear { private set; get; }
        [ShowInInspector, ReadOnly] public PackageGroup RightArmGear { private set; get; }
        [ShowInInspector, ReadOnly] public PackageGroup LeftLegGear { private set; get; }
        [ShowInInspector, ReadOnly] public PackageGroup RightLegGear { private set; get; }

        public List<PackageGroup> Gears
        {
            get
            {
                var gears = new List<PackageGroup>();
                if (HeadGear != null)
                {
                    gears.Add(HeadGear);
                }

                if (TorsoGear != null)
                {
                    gears.Add(TorsoGear);
                }

                if (LeftArmGear != null)
                {
                    gears.Add(LeftArmGear);
                }

                if (RightArmGear != null)
                {
                    gears.Add(RightArmGear);
                }

                if (LeftLegGear != null)
                {
                    gears.Add(LeftLegGear);
                }

                if (RightLegGear != null)
                {
                    gears.Add(RightLegGear);
                }

                return gears;
            }
        }

        public event System.Action<PackageGroup> OnPackageGroupAdded;
        public event System.Action<PackageGroup> OnPackageGroupRemoved;
        public event System.Action<PackageGroup> OnPackageGroupChanged;

        public event System.Action OnWeaponOrGearChanged;

        private PackageInfoContainer PackageInfoContainer =>
            GameApplication.Instance.ExcelBinaryManager.GetContainer<PackageInfoContainer>();

        private HumanoidAppearanceWeaponInfoContainer WeaponInfoContainer =>
            GameApplication.Instance.ExcelBinaryManager.GetContainer<HumanoidAppearanceWeaponInfoContainer>();

        private HumanoidAppearanceGearInfoContainer GearInfoContainer =>
            GameApplication.Instance.ExcelBinaryManager.GetContainer<HumanoidAppearanceGearInfoContainer>();

        [Inject] private GameManager _gameManager;
        [Inject] private HumanoidWeaponManager _weaponManager;

        private void Awake()
        {
            GameApplication.Instance.ArchiveManager.Register(this);
            _gameManager.OnPlayerCreated += HandlePlayerCreated;
            _gameManager.OnPlayerDestroyed += HandlePlayerDestroyed;
        }

        private void OnDestroy()
        {
            GameApplication.Instance?.ArchiveManager.Unregister(this);
            _gameManager.OnPlayerCreated -= HandlePlayerCreated;
            _gameManager.OnPlayerDestroyed -= HandlePlayerDestroyed;
            if (_gameManager.Player)
            {
                HandlePlayerDestroyed(_gameManager.Player);
            }
            
            // 清空所有背包物品
            ClearAllPackages();
        }

        /// <summary>
        /// 获取背包的物品数量
        /// </summary>
        /// <param name="packageId"></param>
        /// <returns></returns>
        public int GetPackageCount(int packageId)
        {
            var count = 0;
            _packageGroups.ForEach(packageData =>
            {
                if (packageData.Data.Id != packageId)
                {
                    return;
                }

                count += packageData.Number;
            });
            return count;
        }

        /// <summary>
        /// 添加物品到背包中，根据传入参数执行部分添加和整体添加的逻辑，并返回操作结果
        /// </summary>
        /// <param name="packageId"></param>
        /// <param name="number"></param>
        /// <param name="forceAddAll"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public bool AddPackage(int packageId, int number, bool forceAddAll)
        {
            if (!PackageInfoContainer.Data.TryGetValue(packageId, out var packageInfoData))
            {
                throw new ArgumentException(
                    $"The PackageInfoData whose id is {packageId} is not existed in PackageInfoContainer");
            }

            // 获取已有的同一物品的不同组
            var packageGroups = new List<PackageGroup>();
            foreach (var packageGroup in _packageGroups)
            {
                if (packageGroup.Data.Id != packageId)
                {
                    continue;
                }

                packageGroups.Add(packageGroup);
            }

            return AddAndCreatePackageGroupInternal(packageInfoData, packageGroups, number, forceAddAll);
        }

        /// <summary>
        /// 从背包中删除物品，不指定物品组
        /// </summary>
        /// <param name="packageId"></param>
        /// <param name="number"></param>
        /// <exception cref="ArgumentException"></exception>
        public void DeletePackage(int packageId, int number = int.MaxValue)
        {
            if (!PackageInfoContainer.Data.TryGetValue(packageId, out var packageInfoData))
            {
                throw new ArgumentException(
                    $"The PackageInfoData whose id is {packageId} is not existed in PackageInfoContainer");
            }

            var index = 0;
            var deleteNumber = 0;
            while (index < _packageGroups.Count)
            {
                var packageData = _packageGroups[index];
                if (packageData.Data.Id != packageId)
                {
                    index++;
                    continue;
                }

                if (number < packageData.Number)
                {
                    deleteNumber += number;
                    packageData.Number -= number;
                    OnPackageGroupChanged?.Invoke(packageData);
                    break;
                }
                else
                {
                    deleteNumber += packageData.Number;
                    packageData.Number = 0;
                    _packageGroups.RemoveAt(index);
                    OnPackageGroupRemoved?.Invoke(packageData);
                }
            }

            if (deleteNumber > 0)
            {
                var packageData = packageInfoData.ToPackageData(
                    HumanoidWeaponSingletonConfigSO.Instance.GetWeaponEquipmentConfiguration,
                    _weaponManager.GetWeaponAttackConfiguration,
                    _weaponManager.GetWeaponDefenceConfiguration,
                    WeaponInfoContainer,
                    GearInfoContainer
                );
                GameApplication.Instance.EventCenter.TriggerEvent<NotificationLostEventParameter>(
                    GameEvents.NotificationLost,
                    new NotificationLostEventParameter
                    {
                        ThumbnailAtlas = packageData.ThumbnailAtlas,
                        ThumbnailName = packageData.ThumbnailName,
                        Name = packageData.Name,
                        Number = deleteNumber
                    });
            }
        }

        /// <summary>
        /// 从背包中删除指定的物品组
        /// </summary>
        /// <param name="groupId"></param>
        /// <param name="number"></param>
        public void DeletePackageGroup(string groupId, int number = int.MaxValue)
        {
            var index = -1;
            for (var i = 0; i < _packageGroups.Count; i++)
            {
                if (_packageGroups[i].GroupId == groupId)
                {
                    index = i;
                    break;
                }
            }

            if (index == -1) return;

            var packageData = _packageGroups[index];
            var deleteNumber = 0;
            if (number < packageData.Number)
            {
                deleteNumber += number;
                packageData.Number -= number;
                OnPackageGroupChanged?.Invoke(packageData);
            }
            else
            {
                deleteNumber += packageData.Number;
                packageData.Number = 0;
                _packageGroups.RemoveAt(index);
                OnPackageGroupRemoved?.Invoke(packageData);
            }

            if (deleteNumber > 0)
            {
                GameApplication.Instance.EventCenter.TriggerEvent<NotificationLostEventParameter>(
                    GameEvents.NotificationLost,
                    new NotificationLostEventParameter
                    {
                        ThumbnailAtlas = packageData.Data.ThumbnailAtlas,
                        ThumbnailName = packageData.Data.ThumbnailName,
                        Name = packageData.Data.Name,
                        Number = deleteNumber
                    });
            }
        }

        public bool IsGroupEquipped(string groupId)
        {
            return LeftHandWeapon?.GroupId == groupId ||
                   RightHandWeapon?.GroupId == groupId ||
                   HeadGear?.GroupId == groupId ||
                   TorsoGear?.GroupId == groupId ||
                   LeftArmGear?.GroupId == groupId ||
                   RightArmGear?.GroupId == groupId ||
                   LeftLegGear?.GroupId == groupId ||
                   RightLegGear?.GroupId == groupId;
        }

        public bool IsPackageEquipped(int packageId)
        {
            return LeftHandWeapon?.Data.Id == packageId ||
                   RightHandWeapon?.Data.Id == packageId ||
                   HeadGear?.Data.Id == packageId ||
                   TorsoGear?.Data.Id == packageId ||
                   LeftArmGear?.Data.Id == packageId ||
                   RightArmGear?.Data.Id == packageId ||
                   LeftLegGear?.Data.Id == packageId ||
                   RightLegGear?.Data.Id == packageId;
        }

        private bool AddAndCreatePackageGroupInternal(
            PackageInfoData packageInfoData,
            List<PackageGroup> existedPackageGroups,
            int number,
            bool forceAddAll
        )
        {
            if (number <= 0)
            {
                return true;
            }

            // 记录实际获得的数量和是否不能获得更多物品
            var addNumber = 0;
            var notGetMore = false;
            var addSuccess = true;

            var packageData = packageInfoData.ToPackageData(
                HumanoidWeaponSingletonConfigSO.Instance.GetWeaponEquipmentConfiguration,
                _weaponManager.GetWeaponAttackConfiguration,
                _weaponManager.GetWeaponDefenceConfiguration,
                WeaponInfoContainer,
                GearInfoContainer
            );
            // 判断物品组是否存在限制
            if (packageInfoData.GetPackageQuantitativeRestriction() == PackageQuantitativeRestriction.OnlyOneGroup)
            {
                // 如果是仅允许有一个物品组，则判断当前是否存在物品组，并添加物品数量，如果数量超过限制就发送拒绝事件
                if (existedPackageGroups.Count == 0)
                {
                    var packageGroup = PackageGroup.CreateNew(packageData, 0);
                    if (number <= packageData.GroupMaximum)
                    {
                        packageGroup.Number = number;
                        addNumber += number;
                        _packageGroups.Add(packageGroup);
                        OnPackageGroupAdded?.Invoke(packageGroup);
                    }
                    else
                    {
                        // 物品数量超出限制，就判断本次操作是否强制添加所有物品，是则视为添加失败
                        if (forceAddAll)
                        {
                            notGetMore = true;
                            addSuccess = false;
                        }
                        else
                        {
                            packageGroup.Number = packageData.GroupMaximum;
                            addNumber += packageData.GroupMaximum;
                            notGetMore = true;
                            _packageGroups.Add(packageGroup);
                            OnPackageGroupAdded?.Invoke(packageGroup);
                        }
                    }
                }
                else
                {
                    var packageGroup = existedPackageGroups[0];
                    // 根据已存在物品组的数量限制执行不同逻辑
                    if (packageGroup.Number >= packageGroup.Data.GroupMaximum)
                    {
                        notGetMore = true;
                        addSuccess = false;
                    }
                    else
                    {
                        var gap = packageGroup.Data.GroupMaximum - packageGroup.Number;
                        if (number <= gap)
                        {
                            packageGroup.Number += number;
                            addNumber += number;
                            OnPackageGroupChanged?.Invoke(packageGroup);
                        }
                        else
                        {
                            // 物品数量超出限制，就判断本次操作是否强制添加所有物品，是则视为添加失败
                            if (forceAddAll)
                            {
                                notGetMore = true;
                                addSuccess = false;
                            }
                            else
                            {
                                packageGroup.Number += gap;
                                addNumber += gap;
                                notGetMore = true;
                                OnPackageGroupChanged?.Invoke(packageGroup);
                            }
                        }
                    }
                }
            }
            else
            {
                // 如果允许有多个组，就将数量拆分为每组允许的数量依次添加，先添加到已有组，再创建新组
                // 这里是添加到已有组
                foreach (var packageGroup in existedPackageGroups)
                {
                    if (packageGroup.Number >= packageGroup.Data.GroupMaximum)
                    {
                        continue;
                    }

                    var gap = packageGroup.Data.GroupMaximum - packageGroup.Number;
                    if (number <= gap)
                    {
                        packageGroup.Number += number;
                        addNumber += number;
                    }
                    else
                    {
                        packageGroup.Number += gap;
                        addNumber += gap;
                    }

                    OnPackageGroupChanged?.Invoke(packageGroup);
                    number -= gap;
                    if (number <= 0)
                    {
                        break;
                    }
                }

                // 这里是创建新组
                while (number > 0)
                {
                    var packageGroup = PackageGroup.CreateNew(packageData, 0);
                    if (number <= packageData.GroupMaximum)
                    {
                        packageGroup.Number = number;
                        addNumber += number;
                    }
                    else
                    {
                        packageGroup.Number = packageData.GroupMaximum;
                        addNumber += packageData.GroupMaximum;
                    }

                    _packageGroups.Add(packageGroup);
                    OnPackageGroupAdded?.Invoke(packageGroup);
                    number -= packageData.GroupMaximum;
                }
            }

            if (addNumber > 0)
            {
                GameApplication.Instance.EventCenter.TriggerEvent<NotificationGetEventParameter>(
                    GameEvents.NotificationGet,
                    new NotificationGetEventParameter
                    {
                        ThumbnailAtlas = packageData.ThumbnailAtlas,
                        ThumbnailName = packageData.ThumbnailName,
                        Name = packageData.Name,
                        Number = addNumber
                    });
            }

            if (notGetMore)
            {
                GameApplication.Instance.EventCenter.TriggerEvent<NotificationNotGetMoreEventParameter>(
                    GameEvents.NotificationNotGetMore,
                    new NotificationNotGetMoreEventParameter
                    {
                        ThumbnailAtlas = packageData.ThumbnailAtlas,
                        ThumbnailName = packageData.ThumbnailName,
                        Name = packageData.Name,
                    });
            }

            return addSuccess;
        }

        public void Save(ArchiveData archiveData)
        {
            archiveData.package.packages =
                _packageGroups.Select(packageData => packageData.ToItemArchiveData()).ToList();
            archiveData.package.leftHandWeaponGroupId = LeftHandWeapon != null ? LeftHandWeapon.GroupId : "";
            archiveData.package.rightHandWeaponGroupId = RightHandWeapon != null ? RightHandWeapon.GroupId : "";
            archiveData.package.headGearGroupId = HeadGear != null ? HeadGear.GroupId : "";
            archiveData.package.torsoGearGroupId = TorsoGear != null ? TorsoGear.GroupId : "";
            archiveData.package.leftArmGearGroupId = LeftArmGear != null ? LeftArmGear.GroupId : "";
            archiveData.package.rightArmGearGroupId = RightArmGear != null ? RightArmGear.GroupId : "";
            archiveData.package.leftLegGearGroupId = LeftLegGear != null ? LeftLegGear.GroupId : "";
            archiveData.package.rightLegGearGroupId = RightLegGear != null ? RightLegGear.GroupId : "";
        }

        public void Load(ArchiveData archiveData)
        {
            // 清空所有背包物品
            ClearAllPackages();
            // 重置背包物品列表
            _packageGroups.AddRange(archiveData.package.packages.Select(packageData =>
                packageData.ToPackageGroup(
                    PackageInfoContainer,
                    HumanoidWeaponSingletonConfigSO.Instance.GetWeaponEquipmentConfiguration,
                    _weaponManager.GetWeaponAttackConfiguration,
                    _weaponManager.GetWeaponDefenceConfiguration,
                    WeaponInfoContainer,
                    GearInfoContainer
                )
            ));
            // 设置武器和装备，但这里仅设置不发送事件，由创建玩家角色时读取并添加，而不是在这里添加至角色身上
            foreach (var packageGroup in _packageGroups)
            {
                if (packageGroup.GroupId == archiveData.package.leftHandWeaponGroupId)
                {
                    LeftHandWeapon = packageGroup;
                }

                if (packageGroup.GroupId == archiveData.package.rightHandWeaponGroupId)
                {
                    RightHandWeapon = packageGroup;
                }

                if (packageGroup.GroupId == archiveData.package.headGearGroupId)
                {
                    HeadGear = packageGroup;
                }

                if (packageGroup.GroupId == archiveData.package.torsoGearGroupId)
                {
                    TorsoGear = packageGroup;
                }

                if (packageGroup.GroupId == archiveData.package.leftArmGearGroupId)
                {
                    LeftArmGear = packageGroup;
                }

                if (packageGroup.GroupId == archiveData.package.rightArmGearGroupId)
                {
                    RightArmGear = packageGroup;
                }

                if (packageGroup.GroupId == archiveData.package.leftLegGearGroupId)
                {
                    LeftLegGear = packageGroup;
                }

                if (packageGroup.GroupId == archiveData.package.rightLegGearGroupId)
                {
                    RightLegGear = packageGroup;
                }
            }
        }

        private void ClearAllPackages()
        {
            // 清空武器和装备
            LeftHandWeapon = null;
            RightHandWeapon = null;
            HeadGear = null;
            TorsoGear = null;
            LeftArmGear = null;
            RightArmGear = null;
            LeftLegGear = null;
            RightLegGear = null;

            // 重置背包物品列表
            _packageGroups.Clear();
        }

        private void HandlePlayerCreated(PlayerCharacterObject player)
        {
            // 监听武器改变事件
            if (player.WeaponAbility)
            {
                player.WeaponAbility.OnWeaponBarChanged += OnPlayerWeaponBarChanged;
            }

            // 监听装备改变事件
            if (player.EquipmentAbility)
            {
                player.EquipmentAbility.OnGearsChanged += OnPlayerGearsChanged;
            }
        }

        private void HandlePlayerDestroyed(PlayerCharacterObject player)
        {
            // 取消监听武器改变事件
            if (player.WeaponAbility)
            {
                player.WeaponAbility.OnWeaponBarChanged -= OnPlayerWeaponBarChanged;
            }

            // 取消监听装备改变事件
            if (player.EquipmentAbility)
            {
                player.EquipmentAbility.OnGearsChanged -= OnPlayerGearsChanged;
            }
        }

        private void OnPlayerWeaponBarChanged(HumanoidCharacterObject player)
        {
            LeftHandWeapon = null;
            RightHandWeapon = null;
            LeftHandWeapon = player.WeaponAbility.LeftHandWeaponSlot?.PackageGroup;
            RightHandWeapon = player.WeaponAbility.RightHandWeaponSlot?.PackageGroup;
            OnWeaponOrGearChanged?.Invoke();
        }

        private void OnPlayerGearsChanged(HumanoidCharacterObject player)
        {
            HeadGear = player.EquipmentAbility.HeadGear;
            TorsoGear = player.EquipmentAbility.TorsoGear;
            LeftArmGear = player.EquipmentAbility.LeftArmGear;
            RightArmGear = player.EquipmentAbility.RightArmGear;
            LeftLegGear = player.EquipmentAbility.LeftLegGear;
            RightLegGear = player.EquipmentAbility.RightLegGear;
            OnWeaponOrGearChanged?.Invoke();
        }

        private void SetPlayerWeaponsAndGears(PlayerCharacterObject player)
        {
            var leftHandWeapon = LeftHandWeapon;
            var rightHandWeapon = RightHandWeapon;
            var headGear = HeadGear;
            var torsoGear = TorsoGear;
            var leftArmGear = LeftArmGear;
            var rightArmGear = RightArmGear;
            var leftLegGear = LeftLegGear;
            var rightLegGear = RightLegGear;

            LeftHandWeapon = null;
            RightHandWeapon = null;
            HeadGear = null;
            TorsoGear = null;
            LeftArmGear = null;
            RightArmGear = null;
            LeftLegGear = null;
            RightLegGear = null;
            OnWeaponOrGearChanged?.Invoke();

            if (leftHandWeapon != null)
            {
                player.WeaponAbility?.AddWeapon(leftHandWeapon);
            }

            if (rightHandWeapon != null)
            {
                player.WeaponAbility?.AddWeapon(rightHandWeapon);
            }

            if (headGear != null)
            {
                player.EquipmentAbility?.EquipGear(headGear);
            }

            if (torsoGear != null)
            {
                player.EquipmentAbility?.EquipGear(torsoGear);
            }

            if (leftArmGear != null)
            {
                player.EquipmentAbility?.EquipGear(leftArmGear);
            }

            if (rightArmGear != null)
            {
                player.EquipmentAbility?.EquipGear(rightArmGear);
            }

            if (leftLegGear != null)
            {
                player.EquipmentAbility?.EquipGear(leftLegGear);
            }

            if (rightLegGear != null)
            {
                player.EquipmentAbility?.EquipGear(rightLegGear);
            }
        }

        [Button("输出物品列表")]
        private void LogPackageList()
        {
            _packageGroups.ForEach(packageGroup =>
            {
                DebugUtil.LogCyan(
                    $"物品名称：{packageGroup.Data.Name}，物品组ID：{packageGroup.GroupId}，物品ID：{packageGroup.Data.Id}，数量：{packageGroup.Number}");
            });
        }
    }
}