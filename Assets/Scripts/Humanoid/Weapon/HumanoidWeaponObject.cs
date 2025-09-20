using System;
using Humanoid.Weapon.Data;
using Package.Data;
using Rendering;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using UnityEngine;
using UnityEngine.Serialization;

namespace Humanoid.Weapon
{
    public class HumanoidWeaponObject : MonoBehaviour
    {
        [Title("未装备/已装备持有父对象位置")] [SerializeField, ReadOnly]
        private Transform unequippedHolderTransform;

        [SerializeField, ReadOnly] private Transform equippedHolderTransform;

        private PackageWeaponData _data;
        public PackageWeaponData Data => _data;

        [Title("武器数据")]
        [ShowInInspector, ReadOnly]
        private string name => Data.Name;

        [SerializeField, ReadOnly] private bool equipped;

        [SerializeField, ReadOnly] [InfoBox("用于提供动作的绑定碰撞体检测")]
        private Collider weaponCollider;

        public Collider WeaponCollider => weaponCollider;

        public bool Equipped
        {
            set
            {
                equipped = value;
                // 改变武器父对象以及位置
                if (equipped)
                {
                    transform.parent = equippedHolderTransform;
                    transform.localPosition = _data.Equipment.equippedLocalPosition;
                    transform.localRotation = Quaternion.Euler(_data.Equipment.equippedLocalRotation);
                }
                else
                {
                    transform.parent = unequippedHolderTransform;
                    transform.localPosition = _data.Equipment.unequippedLocalPosition;
                    transform.localRotation = Quaternion.Euler(_data.Equipment.unequippedLocalRotation);
                }
            }
            get => equipped;
        }

        public void Init(
            PackageWeaponData data,
            Transform unequippedHolderTransform,
            Transform equippedHolderTransform
        )
        {
            this._data = data;
            this.unequippedHolderTransform = unequippedHolderTransform;
            this.equippedHolderTransform = equippedHolderTransform;
            // 设置武器未装备
            Equipped = false;
        }
    }
}