using System;
using System.Collections.Generic;
using System.Linq;
using Character.Ability;
using Character.Data;
using Framework.Core.Extension;
using Humanoid.Weapon;
using Humanoid.Weapon.Data;
using Humanoid.Weapon.SO;
using Package.Data;
using Package.Runtime;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using Skill.Runtime;
using UnityEngine;
using VContainer;

namespace Humanoid.Ability
{
    [Serializable]
    public class HumanoidWeaponSlot
    {
        public PackageGroup PackageGroup; // 武器的物品组
        public bool Equipped; // 注意，这里的装备和武器数据中的装备不同，指的是角色特有的装上和卸下状态
        public HumanoidWeaponObject Object; // 异步加载中为空，加载完毕后为武器游戏对象

        public PackageWeaponData Data => Object != null ? Object.Data : PackageGroup.Data as PackageWeaponData;
    }

    public class HumanoidWeaponAbility : BaseCharacterOptionalAbility
    {
        private new HumanoidCharacterObject Owner => base.Owner as HumanoidCharacterObject;

        [Title("武器配置")] [SerializeField] private Transform leftHipUnequippedHolderTransform;
        [SerializeField] private Transform rightHipUnequippedHolderTransform;
        [SerializeField] private Transform leftHandEquippedHolderTransform;
        [SerializeField] private Transform rightHandEquippedHolderTransform;

        [Title("武器栏")] [SerializeField, ReadOnly]
        private HumanoidWeaponSlot[]
            weaponBar = Array.Empty<HumanoidWeaponSlot>(); // 武器栏，长度0代表无武器，长度1代表双手武器，长度2代表存在左右手武器的可能（允许null即空数据占位）

        [ShowInInspector, ReadOnly] public HumanoidWeaponSlot LeftHandWeaponSlot { get; private set; } // 左手武器槽
        [ShowInInspector, ReadOnly] public HumanoidWeaponSlot RightHandWeaponSlot { get; private set; } // 右手武器槽
        [ShowInInspector, ReadOnly] public HumanoidWeaponSlot AggressiveWeaponSlot { get; private set; } // 攻击武器
        [ShowInInspector, ReadOnly] public HumanoidWeaponSlot DefensiveWeaponSlot { get; private set; } // 防御武器

        private readonly HashSet<string> _weaponBarSkills = new(); // 武器栏对应的技能

        public event System.Action<HumanoidCharacterObject> OnWeaponBarChanged; // 武器栏改变事件

        private HumanoidWeaponCreator _weaponCreator;

        protected override void OnInit()
        {
            base.OnInit();
            // 初始化武器创建类
            _weaponCreator =
                new HumanoidWeaponCreator(HumanoidWeaponSingletonConfigSO.Instance.GetWeaponAppearanceMaterial);
        }

        protected override void OnDispose()
        {
            base.OnDispose();
            // 销毁武器创建类
            _weaponCreator?.Destroy();
            // 销毁武器槽对象
            weaponBar.ForEach(slot =>
            {
                if (slot != null && slot.Object != null && !slot.Object.IsGameObjectDestroyed())
                {
                    GameObject.Destroy(slot.Object.gameObject);
                }
            });
            weaponBar = Array.Empty<HumanoidWeaponSlot>();
            LeftHandWeaponSlot = null;
            RightHandWeaponSlot = null;
            AggressiveWeaponSlot = null;
            DefensiveWeaponSlot = null;
        }

        public void AddWeapon(PackageGroup packageGroup)
        {
            if (packageGroup.Data is not PackageWeaponData packageWeaponData)
            {
                return;
            }

            // 在设置武器前重置武器栏
            ResetWeaponBarByTag(packageWeaponData.Equipment.tag);

            // 添加武器，这里先设置武器栏的数据，实际武器对象创建需要一定时间，等创建后再设置对象
            switch (packageWeaponData.Equipment.tag)
            {
                case HumanoidWeaponTag.LeftHandWeapon:
                {
                    weaponBar[0] = new HumanoidWeaponSlot
                    {
                        PackageGroup = packageGroup,
                        Equipped = false,
                        Object = null
                    };
                }
                    break;
                case HumanoidWeaponTag.RightHandWeapon:
                {
                    weaponBar[1] = new HumanoidWeaponSlot
                    {
                        PackageGroup = packageGroup,
                        Equipped = false,
                        Object = null
                    };
                }
                    break;
                case HumanoidWeaponTag.TwoHandsWeapon:
                {
                    weaponBar[0] = new HumanoidWeaponSlot
                    {
                        PackageGroup = packageGroup,
                        Equipped = false,
                        Object = null
                    };
                }
                    break;
            }

            // 重新计算武器属性
            ResetProperty();
            // 调整玩家姿势
            AdjustPose();
            // 更新武器用途
            UpdateWeaponUsage();
            // 更新武器技能
            UpdateWeaponSkills();
            // 发送事件
            OnWeaponBarChanged?.Invoke(Owner);

            // 异步创建武器对象
            _weaponCreator.CreateWeaponObjectAsync(
                packageWeaponData,
                packageWeaponData.Equipment.unequippedPosition == HumanoidWeaponUnequippedPosition.LeftHip
                    ? leftHipUnequippedHolderTransform
                    : rightHipUnequippedHolderTransform,
                packageWeaponData.Equipment.equippedPosition == HumanoidWeaponEquippedPosition.LeftHand
                    ? leftHandEquippedHolderTransform
                    : rightHandEquippedHolderTransform,
                newWeaponObject =>
                {
                    // 由于创建武器对象是异步的，所以这里需要再次比较数据是否一致，不一致即代表数据过期，不予设置
                    var match = false;
                    weaponBar.ForEach(slot =>
                    {
                        if (slot == null || slot.PackageGroup.Data != newWeaponObject.Data) return;
                        match = true;
                        // 如果武器槽已经存在武器对象，就销毁原武器对象
                        if (slot.Object)
                        {
                            GameObject.Destroy(slot.Object.gameObject);
                        }
                        slot.Object = newWeaponObject;
                        newWeaponObject.Equipped = slot.Equipped;
                    });
                    // 如果没有匹配到就销毁对象
                    if (!match)
                    {
                        GameObject.Destroy(newWeaponObject.gameObject);
                    }
                });
        }

        public void RemoveWeapon(PackageGroup packageGroup)
        {
            if (packageGroup.Data is not PackageWeaponData packageWeaponData)
            {
                return;
            }

            HumanoidWeaponSlot removedSlot = null;
            foreach (var weaponSlot in weaponBar)
            {
                if (weaponSlot != null && weaponSlot.PackageGroup == packageGroup)
                {
                    removedSlot = weaponSlot;
                    break;
                }
            }

            if (removedSlot == null)
            {
                return;
            }

            // 重置武器栏
            ResetWeaponBarByTag(packageWeaponData.Equipment.tag);
            // 重新计算武器属性
            ResetProperty();
            // 调整玩家姿势
            AdjustPose();
            // 更新武器用途
            UpdateWeaponUsage();
            // 更新武器技能
            UpdateWeaponSkills();
            // 发送事件
            OnWeaponBarChanged?.Invoke(Owner);
        }

        public void ClearWeaponBar()
        {
            // 清空武器栏
            weaponBar.ForEach(slot => GameObject.Destroy(slot?.Object?.gameObject));
            weaponBar = Array.Empty<HumanoidWeaponSlot>();
            // 重新计算武器属性
            ResetProperty();
            // 调整玩家姿势
            AdjustPose();
            // 更新武器用途
            UpdateWeaponUsage();
            // 更新武器技能
            UpdateWeaponSkills();
            // 发送事件
            OnWeaponBarChanged?.Invoke(Owner);
        }

        public HumanoidCharacterPose CalculatePoseIfWeaponsAreEquipped()
        {
            return CalculatePose(true);
        }

        public bool EquipByEquippedPosition(HumanoidWeaponEquippedPosition equippedPosition)
        {
            var equippedNumber = 0;
            weaponBar.ForEach(slot =>
            {
                if (slot == null || slot.PackageGroup.Data is not PackageWeaponData packageWeaponData)
                {
                    return;
                }

                if (packageWeaponData.Equipment.equippedPosition == equippedPosition && !slot.Equipped)
                {
                    slot.Equipped = true;
                    if (slot.Object)
                    {
                        slot.Object.Equipped = true;
                    }

                    equippedNumber++;
                }
            });

            if (equippedNumber > 0)
            {
                // 调整玩家姿势
                AdjustPose();
                // 更新武器用途
                UpdateWeaponUsage();
                // 更新武器技能
                UpdateWeaponSkills();
                return true;
            }

            return false;
        }

        public bool UnequipByUnequippedPosition(HumanoidWeaponEquippedPosition equippedPosition)
        {
            var unequippedNumber = 0;
            weaponBar.ForEach(slot =>
            {
                if (slot == null || slot.PackageGroup.Data is not PackageWeaponData packageWeaponData)
                {
                    return;
                }

                if (packageWeaponData.Equipment.equippedPosition == equippedPosition && slot.Equipped)
                {
                    slot.Equipped = false;
                    if (slot.Object)
                    {
                        slot.Object.Equipped = false;
                    }

                    unequippedNumber++;
                }
            });

            if (unequippedNumber > 0)
            {
                // 调整玩家姿势
                AdjustPose();
                // 更新武器用途
                UpdateWeaponUsage();
                // 更新武器技能
                UpdateWeaponSkills();
                return true;
            }

            return false;
        }

        /// <summary>
        /// 通过标签重置武器栏
        /// </summary>
        /// <param name="weaponTag">武器标签</param>
        private void ResetWeaponBarByTag(HumanoidWeaponTag weaponTag)
        {
            switch (weaponTag)
            {
                case HumanoidWeaponTag.LeftHandWeapon: // 左手武器标签重置左手武器或双手武器
                {
                    if (weaponBar.Length != 2)
                    {
                        // 重置双手武器
                        weaponBar.ForEach(slot =>
                        {
                            if (slot == null)
                            {
                                return;
                            }

                            GameObject.Destroy(slot.Object?.gameObject);
                        });
                        weaponBar = new HumanoidWeaponSlot[2];
                    }
                    else
                    {
                        // 重置左手武器
                        if (weaponBar[0] != null)
                        {
                            GameObject.Destroy(weaponBar[0].Object?.gameObject);
                            weaponBar[0] = null;
                        }
                    }
                }
                    break;
                case HumanoidWeaponTag.RightHandWeapon: // 右手武器标签重置右手武器或双手武器
                {
                    if (weaponBar.Length != 2)
                    {
                        // 重置双手武器
                        weaponBar.ForEach(slot =>
                        {
                            if (slot == null)
                            {
                                return;
                            }

                            GameObject.Destroy(slot.Object?.gameObject);
                        });
                        weaponBar = new HumanoidWeaponSlot[2];
                    }
                    else
                    {
                        // 重置右手武器
                        if (weaponBar[1] != null)
                        {
                            GameObject.Destroy(weaponBar[1].Object?.gameObject);
                            weaponBar[1] = null;
                        }
                    }
                }
                    break;
                case HumanoidWeaponTag.TwoHandsWeapon: // 双手武器标签重置所有类型武器
                {
                    if (weaponBar.Length != 1)
                    {
                        weaponBar.ForEach(slot =>
                        {
                            if (slot == null)
                            {
                                return;
                            }

                            GameObject.Destroy(slot.Object?.gameObject);
                        });
                        weaponBar = new HumanoidWeaponSlot[1];
                    }
                    else
                    {
                        if (weaponBar[0] != null)
                        {
                            GameObject.Destroy(weaponBar[0].Object?.gameObject);
                            weaponBar[0] = null;
                        }
                    }
                }
                    break;
            }
        }

        /// <summary>
        /// 重置武器属性
        /// </summary>
        private void ResetProperty()
        {
            var property = CharacterProperty.Zero;
            weaponBar.ForEach(slot =>
            {
                if (slot == null || slot.PackageGroup.Data is not PackageWeaponData packageWeaponData)
                {
                    return;
                }

                property += new CharacterProperty
                {
                    maxHp = packageWeaponData.MaxHp,
                    maxMp = packageWeaponData.MaxMp,
                    stamina = packageWeaponData.Stamina,
                    strength = packageWeaponData.Strength,
                    magic = packageWeaponData.Magic,
                    reaction = packageWeaponData.Reaction,
                    luck = packageWeaponData.Luck,
                };
            });

            Owner.HumanoidParameters.weaponProperty = property;
            Owner.PropertyAbility.CheckProperty();
        }

        /// <summary>
        /// 调整武器姿势
        /// </summary>
        private void AdjustPose()
        {
            Owner.SetPose(CalculatePose());
        }

        private HumanoidCharacterPose CalculatePose(bool ignoreEquipped = false)
        {
            // 无武器就采用默认姿势
            if (weaponBar.Length == 0 || weaponBar.All(slot => slot == null))
            {
                return HumanoidCharacterPose.NoWeapon;
            }

            // 有武器就根据武器位置计算姿势
            var pose = HumanoidCharacterPose.NoWeapon;
            if (weaponBar.Length == 2)
            {
                var leftWeaponSlot = weaponBar[0];
                var rightWeaponSlot = weaponBar[1];

                if (leftWeaponSlot != null && (ignoreEquipped || leftWeaponSlot.Equipped))
                {
                    pose = HumanoidCharacterPose.HoldsWeaponInLeftHand;
                }

                if (rightWeaponSlot != null && (ignoreEquipped || rightWeaponSlot.Equipped))
                {
                    pose = pose == HumanoidCharacterPose.HoldsWeaponInLeftHand
                        ? HumanoidCharacterPose.HoldsWeaponInLeftAndRightHand
                        : HumanoidCharacterPose.HoldsWeaponInRightHand;
                }
            }
            else
            {
                pose = weaponBar.Any(slot => slot != null && (ignoreEquipped || slot.Equipped))
                    ? HumanoidCharacterPose.HoldsWeaponInTwoHands
                    : HumanoidCharacterPose.NoWeapon;
            }

            return pose;
        }

        /// <summary>
        /// 更新武器用途
        /// </summary>
        private void UpdateWeaponUsage()
        {
            LeftHandWeaponSlot = null;
            RightHandWeaponSlot = null;
            AggressiveWeaponSlot = null;
            DefensiveWeaponSlot = null;

            switch (weaponBar.Length)
            {
                case 0:
                {
                }
                    break;
                case 1:
                {
                    LeftHandWeaponSlot = weaponBar[0];
                    RightHandWeaponSlot = weaponBar[0];
                }
                    break;
                default:
                {
                    LeftHandWeaponSlot = weaponBar[0];
                    RightHandWeaponSlot = weaponBar[1];
                }
                    break;
            }

            weaponBar.ForEach(slot =>
            {
                if (slot == null || slot.PackageGroup.Data is not PackageWeaponData packageWeaponData)
                {
                    return;
                }

                if (packageWeaponData.Attack.supportAttack)
                {
                    AggressiveWeaponSlot = slot;
                }

                if (packageWeaponData.Defence.supportDefend)
                {
                    DefensiveWeaponSlot = slot;
                }
            });
        }

        /// <summary>
        /// 更新武器技能
        /// </summary>
        private void UpdateWeaponSkills()
        {
            _weaponBarSkills.ForEach(id => Owner.SkillAbility?.DeleteSkill(id, SkillGroup.Dynamic));
            _weaponBarSkills.Clear();
            weaponBar.ForEach(slot =>
            {
                if (slot == null || slot.PackageGroup.Data is not PackageWeaponData packageWeaponData)
                {
                    return;
                }

                if (slot.Equipped)
                {
                    _weaponBarSkills.AddRange(packageWeaponData.Skills);
                }
            });
            _weaponBarSkills.ForEach(id => Owner.SkillAbility?.AddSkill(id, SkillGroup.Dynamic));
        }
    }
}