using System;
using System.Collections.Generic;
using System.Linq;
using Character.Data;
using Features.Game.Data;
using Framework.Core.LiveData;
using Package;
using Package.Data;
using Player;
using Sirenix.Utilities;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using Character.Data;
using Features.Game.Data;
using Framework.Core.LiveData;
using Package;
using Package.Data;
using Package.Data.Extension;
using Package.Runtime;
using Player;
using Sirenix.Utilities;
using UnityEngine;

namespace Features.Game.UI.Package
{
    public enum GamePackageTabType
    {
        All,
        Weapon,
        Gear,
        Item,
        Material,
    }

    [Flags]
    public enum GamePackageTabFlags
    {
        None = 0,
        All = 1 << 0,
        Weapon = 1 << 1,
        Gear = 1 << 2,
        Item = 1 << 3,
        Material = 1 << 4,
    }

    public class GamePackageModel
    {
        private readonly PackageManager _packageManager;
        private readonly int _gridRowNumber;
        private readonly int _minLineNumber;

        private readonly MutableLiveData<GamePackageTabType> _tabTypeLiveData = new();
        public LiveData<GamePackageTabType> GetTabType() => _tabTypeLiveData;

        private readonly List<PackageGroup> _newPackageGroups = new();
        private readonly MutableLiveData<GamePackageTabFlags> _newTabFlagsLiveData = new();
        public LiveData<GamePackageTabFlags> GetNewTabFlags() => _newTabFlagsLiveData;

        private readonly MutableLiveData<List<object>> _tabPackageListLiveData = new();
        public LiveData<List<object>> GetTabPackageList() => _tabPackageListLiveData;

        public GamePackageModel(
            PackageManager packageManager,
            int gridRowNumber,
            int minLineNumber
        )
        {
            _packageManager = packageManager;
            _gridRowNumber = gridRowNumber;
            _minLineNumber = minLineNumber;

            _packageManager.OnPackageGroupAdded += OnPackageGroupAdded;
            _packageManager.OnPackageGroupRemoved += OnPackageGroupRemoved;
            _packageManager.OnPackageGroupChanged += OnPackageGroupChanged;
            _packageManager.OnWeaponOrGearChanged += OnWeaponOrGearChanged;

            // 初始化获取存在新物品的Tab
            _packageManager.PackageGroups.ForEach(packageData =>
            {
                if (!packageData.New) return;
                _newPackageGroups.Add(packageData);
            });
            CheckNewTab();
        }

        public void Destroy()
        {
            _packageManager.OnPackageGroupAdded -= OnPackageGroupAdded;
            _packageManager.OnPackageGroupRemoved -= OnPackageGroupRemoved;
            _packageManager.OnPackageGroupChanged -= OnPackageGroupChanged;
            _packageManager.OnWeaponOrGearChanged -= OnWeaponOrGearChanged;
        }

        public void SwitchTab(GamePackageTabType tabType)
        {
            // 设置Tab类型
            _tabTypeLiveData.SetValue(tabType);
            // 过滤物品列表
            FilterPackageList();
        }

        public void SwitchToPreviousTab()
        {
            var previousValue = (int)_tabTypeLiveData.Value - 1;
            if (previousValue < 0)
            {
                previousValue = Enum.GetValues(typeof(GamePackageTabType)).Length - 1; // 回到最后一个枚举值
            }

            SwitchTab((GamePackageTabType)previousValue);
        }

        public void SwitchToNextTab()
        {
            var nextValue = (int)_tabTypeLiveData.Value + 1;
            if (nextValue >= Enum.GetValues(typeof(GamePackageTabType)).Length)
            {
                nextValue = 0; // 回到第一个枚举值
            }

            SwitchTab((GamePackageTabType)nextValue);
        }

        public bool FocusUpperGrid(int position, out int index, out object data)
        {
            index = -1;
            data = null;
            if (position < 0 || position >= _tabPackageListLiveData.Value.Count || position < _gridRowNumber)
            {
                return false;
            }

            index = position - _gridRowNumber;
            data = _tabPackageListLiveData.Value[index];
            switch (data)
            {
                case GamePackageUIData itemData:
                {
                    itemData.Focused = true;
                }
                    break;
                case GamePackagePlaceholderUIData placeholderData:
                {
                    placeholderData.Focused = true;
                }
                    break;
                default:
                    return false;
            }

            return true;
        }

        public bool FocusLowerGrid(int position, out int index, out object data)
        {
            index = -1;
            data = null;
            if (position < 0 || position >= _tabPackageListLiveData.Value.Count ||
                position >= _tabPackageListLiveData.Value.Count - _gridRowNumber)
            {
                return false;
            }

            index = position + _gridRowNumber;
            data = _tabPackageListLiveData.Value[index];
            switch (data)
            {
                case GamePackageUIData itemData:
                {
                    itemData.Focused = true;
                }
                    break;
                case GamePackagePlaceholderUIData placeholderData:
                {
                    placeholderData.Focused = true;
                }
                    break;
                default:
                    return false;
            }

            return true;
        }

        public bool FocusLeftGrid(int position, out int index, out object data)
        {
            index = -1;
            data = null;
            if (position < 0 || position >= _tabPackageListLiveData.Value.Count || position % _gridRowNumber == 0)
            {
                return false;
            }

            index = position - 1;
            data = _tabPackageListLiveData.Value[index];
            switch (data)
            {
                case GamePackageUIData itemData:
                {
                    itemData.Focused = true;
                }
                    break;
                case GamePackagePlaceholderUIData placeholderData:
                {
                    placeholderData.Focused = true;
                }
                    break;
                default:
                    return false;
            }

            return true;
        }

        public bool FocusRightGrid(int position, out int index, out object data)
        {
            index = -1;
            data = null;
            if (position < 0 || position >= _tabPackageListLiveData.Value.Count ||
                position % _gridRowNumber == _gridRowNumber - 1)
            {
                return false;
            }

            index = position + 1;
            data = _tabPackageListLiveData.Value[index];
            switch (data)
            {
                case GamePackageUIData itemData:
                {
                    itemData.Focused = true;
                }
                    break;
                case GamePackagePlaceholderUIData placeholderData:
                {
                    placeholderData.Focused = true;
                }
                    break;
                default:
                    return false;
            }

            return true;
        }

        public bool MarkPackageSeen(int position, out GamePackageUIData data)
        {
            data = null;
            if (position < 0 || position >= _tabPackageListLiveData.Value.Count)
            {
                return false;
            }

            if (_tabPackageListLiveData.Value[position] is GamePackageUIData packageData && packageData.PackageGroup.New)
            {
                packageData.PackageGroup.New = false;
                _newPackageGroups.Remove(packageData.PackageGroup);
                CheckNewTab();
                data = packageData;
                return true;
            }

            return false;
        }

        public void ToggleMultipleSelect(GamePackageUIData itemUIData)
        {
            itemUIData.MultipleSelected = !itemUIData.MultipleSelected;
        }

        public bool RemoveMultipleSelectedPackages(
            System.Action<int, PackageGroup> successCallback,
            System.Action<int, PackageGroup> failureCallback
        )
        {
            var toRemovePackageGroups = new Dictionary<int, PackageGroup>();
            for (var i = 0; i < _tabPackageListLiveData.Value.Count; i++)
            {
                var item = _tabPackageListLiveData.Value[i];
                if (item is GamePackageUIData itemUIData)
                {
                    if (itemUIData.MultipleSelected)
                    {
                        toRemovePackageGroups.TryAdd(i, itemUIData.PackageGroup);
                    }

                    if (itemUIData.Focused)
                    {
                        toRemovePackageGroups.TryAdd(i, itemUIData.PackageGroup);
                    }
                }
            }

            if (toRemovePackageGroups.Count == 0)
            {
                return false;
            }

            return DeletePackages();

            bool DeletePackages()
            {
                var deleteSuccessNumber = 0;
                foreach (var pair in toRemovePackageGroups)
                {
                    // 过滤装备中的物品组
                    if (_packageManager.IsGroupEquipped(pair.Value.GroupId))
                    {
                        failureCallback.Invoke(pair.Key, pair.Value);
                        continue;
                    }

                    _packageManager.DeletePackageGroup(pair.Value.GroupId);
                    successCallback.Invoke(pair.Key, pair.Value);
                    deleteSuccessNumber++;
                }

                return deleteSuccessNumber == toRemovePackageGroups.Count;
            }
        }

        private void OnPackageGroupAdded(PackageGroup packageGroup)
        {
            // 判断物品是否添加到当前Tab的列表中，不是则不用更新列表
            if (!CheckPackageGroupBelongToCurrentTab(packageGroup))
            {
                return;
            }

            // 添加到新物品中
            if (packageGroup.New)
            {
                _newPackageGroups.Add(packageGroup);
                CheckNewTab();
            }

            // 重置列表
            FilterPackageList();
        }

        private void OnPackageGroupRemoved(PackageGroup packageGroup)
        {
            // 判断物品是否添加到当前Tab的列表中，不是则不用更新列表
            if (!CheckPackageGroupBelongToCurrentTab(packageGroup))
            {
                return;
            }

            // 判断删除物品是否处于新物品中，是则从新物品列表中删除
            if (_newPackageGroups.Contains(packageGroup))
            {
                _newPackageGroups.Remove(packageGroup);
                CheckNewTab();
            }

            // 查找物品组位置，并将其替换成占位符，最后重新设置列表
            var list = new List<object>();
            for (var i = 0; i < _tabPackageListLiveData.Value.Count; i++)
            {
                if (_tabPackageListLiveData.Value[i] is GamePackageUIData itemUIData &&
                    itemUIData.PackageGroup == packageGroup)
                {
                    list.Add(new GamePackagePlaceholderUIData
                    {
                        Focused = itemUIData.Focused,
                    });
                }
                else
                {
                    list.Add(_tabPackageListLiveData.Value[i]);
                }
            }

            _tabPackageListLiveData.SetValue(list);
        }

        private void OnPackageGroupChanged(PackageGroup packageGroup)
        {
            // 判断物品是否添加到当前Tab的列表中，不是则不用更新列表
            if (!CheckPackageGroupBelongToCurrentTab(packageGroup))
            {
                return;
            }

            // 判断物品是否是新物品，是则添加
            if (packageGroup.New && !_newPackageGroups.Contains(packageGroup))
            {
                _newPackageGroups.Add(packageGroup);
                CheckNewTab();
            }

            // 查找物品组位置，并将其替换成最新数据，最后重新设置列表
            var list = new List<object>();
            for (var i = 0; i < _tabPackageListLiveData.Value.Count; i++)
            {
                if (_tabPackageListLiveData.Value[i] is GamePackageUIData itemUIData &&
                    itemUIData.PackageGroup == packageGroup)
                {
                    list.Add(new GamePackageUIData
                    {
                        PackageGroup = packageGroup,
                        Focused = itemUIData.Focused,
                        MultipleSelected = itemUIData.MultipleSelected,
                    });
                }
                else
                {
                    list.Add(_tabPackageListLiveData.Value[i]);
                }
            }

            _tabPackageListLiveData.SetValue(list);
        }

        private void OnWeaponOrGearChanged()
        {
            // 重新设置列表，而不更新数据本身，武器和装备的改变本质是内部引用的数据字段改变
            _tabPackageListLiveData.SetValue(_tabPackageListLiveData.Value.ToList());
        }

        private void CheckNewTab()
        {
            var flags = GamePackageTabFlags.None;
            _newPackageGroups.ForEach(packageGroup =>
            {
                flags |= GamePackageTabFlags.All;
                switch (packageGroup.Data.GetPackageType())
                {
                    case PackageType.Weapon:
                        flags |= GamePackageTabFlags.Weapon;
                        break;
                    case PackageType.Gear:
                        flags |= GamePackageTabFlags.Gear;
                        break;
                    case PackageType.Item:
                        flags |= GamePackageTabFlags.Item;
                        break;
                    case PackageType.Material:
                        flags |= GamePackageTabFlags.Material;
                        break;
                }
            });
            _newTabFlagsLiveData.SetValue(flags);
        }

        private void FilterPackageList()
        {
            // 根据标签筛选需要展示的物品组列表
            SetPackageList(_packageManager.PackageGroups
                .Where(packageGroup =>
                {
                    switch (_tabTypeLiveData.Value)
                    {
                        case GamePackageTabType.All:
                            return true;
                        case GamePackageTabType.Weapon:
                            return packageGroup.Data.GetPackageType() == PackageType.Weapon;
                        case GamePackageTabType.Gear:
                            return packageGroup.Data.GetPackageType() == PackageType.Gear;
                        case GamePackageTabType.Item:
                            return packageGroup.Data.GetPackageType() == PackageType.Item;
                        case GamePackageTabType.Material:
                            return packageGroup.Data.GetPackageType() == PackageType.Material;
                        default:
                            return false;
                    }
                })
                .Select(packageGroup => new GamePackageUIData
                {
                    PackageGroup = packageGroup,
                })
                .ToList()
            );
            return;

            void SetPackageList(List<GamePackageUIData> packageList)
            {
                // 传入物品列表并填充物品列表
                var list = FillPackageList(packageList);
                _tabPackageListLiveData.SetValue(list);
            }

            List<object> FillPackageList(List<GamePackageUIData> packageList)
            {
                // 获取物品格子数量
                var count = packageList.Count < _gridRowNumber * _minLineNumber
                    ? _gridRowNumber * _minLineNumber
                    : Mathf.CeilToInt(1f * packageList.Count / _gridRowNumber) * _gridRowNumber;
                // 填充逻辑是先使用物品填充，再用占位符填充
                var list = new List<object>();
                for (var i = 0; i < count; i++)
                {
                    if (i < packageList.Count)
                    {
                        list.Add(packageList[i]);
                    }
                    else
                    {
                        list.Add(new GamePackagePlaceholderUIData());
                    }
                }

                return list;
            }
        }

        private bool CheckPackageGroupBelongToCurrentTab(PackageGroup packageGroup)
        {
            // 判断物品组是否属于当前标签下
            if (_tabTypeLiveData.Value != GamePackageTabType.All)
            {
                var packageType = packageGroup.Data.GetPackageType();
                if (packageType == PackageType.Weapon && _tabTypeLiveData.Value != GamePackageTabType.Weapon)
                {
                    return false;
                }

                if (packageType == PackageType.Gear && _tabTypeLiveData.Value != GamePackageTabType.Gear)
                {
                    return false;
                }

                if (packageType == PackageType.Item && _tabTypeLiveData.Value != GamePackageTabType.Item)
                {
                    return false;
                }

                if (packageType == PackageType.Material && _tabTypeLiveData.Value != GamePackageTabType.Material)
                {
                    return false;
                }
            }

            return true;
        }
    }
}