using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Humanoid.Weapon.Data
{
    public enum HumanoidWeaponTag
    {
        LeftHandWeapon,
        RightHandWeapon,
        TwoHandsWeapon,
    }

    public enum HumanoidWeaponUnequippedPosition
    {
        LeftHip,
        RightHip,
    }

    public enum HumanoidWeaponEquippedPosition
    {
        LeftHand,
        RightHand,
    }
    
    [Serializable]
    public struct HumanoidWeaponEquipmentConfigData
    {
        [InfoBox("武器标签，影响角色的武器共存逻辑")] public HumanoidWeaponTag tag;

        [Title("装上/卸下位置")] public HumanoidWeaponUnequippedPosition unequippedPosition;
        public Vector3 unequippedLocalPosition;
        public Vector3 unequippedLocalRotation;
        public HumanoidWeaponEquippedPosition equippedPosition;
        public Vector3 equippedLocalPosition;
        public Vector3 equippedLocalRotation;
    }
}