using System;
using System.Collections;
using System.Collections.Generic;
using Buff.Data;
using Framework.Common.Resource;
using Humanoid.Data;
using Humanoid.Weapon.Data;
using Package.Data;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using Skill;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.U2D;

namespace Package.Editor.Data
{
    public class PackageEditorData : ScriptableObject
    {
        [NonSerialized] public HumanoidAppearanceWeaponInfoContainer WeaponInfoContainer;
        [NonSerialized] public HumanoidAppearanceGearInfoContainer GearInfoContainer;
        [NonSerialized] public Action<PackageEditorData, string, string> LoadSprite;

        [Title("物品通用配置"), ReadOnly] public int id;
        public PackageType type;

        [HorizontalGroup("通用", 120f), HideLabel,
         PreviewField(120f, Alignment = ObjectFieldAlignment.Left)]
        [ReadOnly]
        public Sprite thumbnail;

        [VerticalGroup("通用/右侧")] public new string name;
        [VerticalGroup("通用/右侧")] public string introduction;
        [VerticalGroup("通用/右侧")] public int price;

        [VerticalGroup("通用/右侧")] [OnValueChanged("OnThumbnailChanged")]
        public string thumbnailAtlas;

        [VerticalGroup("通用/右侧")] [OnValueChanged("OnThumbnailChanged")]
        public string thumbnailName;

        [VerticalGroup("通用/右侧")] public PackageQuantitativeRestriction quantitativeRestriction;
        [VerticalGroup("通用/右侧")] public int groupMaximum;

        [ShowIfGroup("type", PackageType.Weapon, GroupID = "武器")] [BoxGroup("武器/武器配置")]
        public HumanoidWeaponType weaponType;

        [BoxGroup("武器/武器配置")] [ValueDropdown("GetWeaponAppearances")]
        public int weaponAppearanceId;

        [BoxGroup("武器/武器配置")] [MinValue(0f), MaxValue(1f)]
        public float defenceDamageMultiplier = 1f;

        [BoxGroup("武器/武器配置")] [MinValue(0f), MaxValue(1f)]
        public float defenceBreakResumeSpeed = 1f;

        [ShowIfGroup("type", PackageType.Gear, GroupID = "装备")] [BoxGroup("装备/装备配置")]
        public HumanoidAppearanceGearPart gearPart;

        [BoxGroup("装备/装备配置")] [ValueDropdown("GetGearAppearances")]
        public int gearAppearanceId;

        [BoxGroup("武器/武器配置")] [BoxGroup("装备/装备配置")]
        public int maxHp;

        [BoxGroup("武器/武器配置")] [BoxGroup("装备/装备配置")]
        public int maxMp;

        [BoxGroup("武器/武器配置")] [BoxGroup("装备/装备配置")]
        public int stamina;

        [BoxGroup("武器/武器配置")] [BoxGroup("装备/装备配置")]
        public int strength;

        [BoxGroup("武器/武器配置")] [BoxGroup("装备/装备配置")]
        public int magic;

        [BoxGroup("武器/武器配置")] [BoxGroup("装备/装备配置")]
        public int reaction;

        [BoxGroup("武器/武器配置")] [BoxGroup("装备/装备配置")]
        public int luck;

        [BoxGroup("武器/武器配置")] public List<string> weaponSkills = new();

        [ShowIfGroup("type", PackageType.Item, GroupID = "道具")] [BoxGroup("道具/道具配置")]
        public bool useDefaultItemPrefab;

        [HideIf("useDefaultItemPrefab")] [BoxGroup("道具/道具配置")]
        public string itemAppearancePrefab;

        [BoxGroup("道具/道具资源配置")] public int hp;
        [BoxGroup("道具/道具资源配置")] public int mp;
        [BoxGroup("道具/道具Buff配置")] public string buffId;
        [BoxGroup("道具/道具Buff配置")] public int buffStack;
        [BoxGroup("道具/道具Buff配置")] public float buffDuration;
        [BoxGroup("道具/道具技能配置")] public List<string> itemSkills = new();

        [ShowIfGroup("type", PackageType.Material, GroupID = "材料")] [BoxGroup("材料/材料配置")]
        public bool useDefaultMaterialPrefab;

        [HideIf("useDefaultMaterialPrefab")] [BoxGroup("材料/材料配置")]
        public string materialAppearancePrefab;

        public void CheckThumbnail()
        {
            OnThumbnailChanged();
        }

        private void OnThumbnailChanged()
        {
            LoadSprite?.Invoke(this, thumbnailAtlas, thumbnailName);
        }

        private IEnumerable GetWeaponAppearances()
        {
            var items = new List<ValueDropdownItem>();
            if (WeaponInfoContainer == null)
            {
                return items;
            }

            WeaponInfoContainer.Data.Values.ForEach(weaponInfoData =>
            {
                items.Add(new ValueDropdownItem
                {
                    Text = $"{weaponInfoData.Id}({weaponInfoData.Comment})",
                    Value = weaponInfoData.Id,
                });
            });

            return items;
        }

        private IEnumerable GetGearAppearances()
        {
            var items = new List<ValueDropdownItem>();
            if (GearInfoContainer == null)
            {
                return items;
            }

            GearInfoContainer.Data.Values.ForEach(gearInfoData =>
            {
                items.Add(new ValueDropdownItem
                {
                    Text = $"{gearInfoData.Id}({gearInfoData.Mark})",
                    Value = gearInfoData.Id,
                });
            });

            return items;
        }
    }
}