using System;
using Character.Data;
using Humanoid.Data;
using Sirenix.OdinInspector;

namespace Humanoid
{
    public enum HumanoidCharacterPose
    {
        // 无武器姿势
        NoWeapon,

        // 持有武器后的姿势
        HoldsWeaponInLeftHand,
        HoldsWeaponInRightHand,
        HoldsWeaponInLeftAndRightHand,
        HoldsWeaponInTwoHands,
    }

    public enum HumanoidCharacterRace
    {
        HumanMale,
        HumanFemale,
    }

    [Serializable]
    public class HumanoidCharacterParameters
    {
        [Title("人形角色种族")] public HumanoidCharacterRace race;

        [Title("人形角色姿势")] public HumanoidCharacterPose pose;

        [Title("人形角色战斗")] public CharacterProperty weaponProperty = CharacterProperty.Zero; // 角色武器属性，指角色武器带来的属性
        
        [Title("人形角色战斗")] public CharacterProperty gearProperty = CharacterProperty.Zero; // 角色装备属性，指角色装备带来的属性
        
        [Title("人形角色外观")] public HumanoidAppearanceData appearance = HumanoidAppearanceData.Empty;
        
        public bool WeaponUsed => pose != HumanoidCharacterPose.NoWeapon;
    }
}