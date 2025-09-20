using System;
using System.Collections.Generic;
using Character;
using Character.Data;
using Humanoid.Ability;
using Humanoid.Data;
using Humanoid.Model;
using Humanoid.SO;
using Package.Data;
using Package.Runtime;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace Humanoid
{
    public class HumanoidCharacterObject : CharacterObject
    {
        [Title("人形角色可选能力")]
        public new HumanoidAppearanceAbility AppearanceAbility => (HumanoidAppearanceAbility)base.AppearanceAbility;

        public new HumanoidAnimationAbility AnimationAbility => (HumanoidAnimationAbility)base.AnimationAbility;

        [SerializeField] private HumanoidWeaponAbility weaponAbility;
        public HumanoidWeaponAbility WeaponAbility => weaponAbility;

        [SerializeField] private HumanoidEquipmentAbility equipmentAbility;
        public HumanoidEquipmentAbility EquipmentAbility => equipmentAbility;

        [Title("人形角色数据"), ShowInInspector, ReadOnly]
        public HumanoidCharacterParameters HumanoidParameters { get; } = new();

        protected override void Awake()
        {
            base.Awake();
            PropertyAbility = new HumanoidPropertyAbility(AlgorithmManager);
        }

        protected override void InitOptionalAbility()
        {
            base.InitOptionalAbility();
            weaponAbility?.Init(this);
            equipmentAbility?.Init(this);
        }

        protected override void DestroyOptionalAbility()
        {
            base.DestroyOptionalAbility();
            weaponAbility?.Dispose();
            equipmentAbility?.Dispose();
        }

        public void SetHumanoidCharacterParameters(
            HumanoidCharacterRace race,
            List<PackageGroup> weapons,
            List<PackageGroup> gears
        )
        {
            SetRace(race);
            SetPose(HumanoidCharacterPose.NoWeapon);
            // 装备武器和套装
            weapons.ForEach(weapon => weaponAbility?.AddWeapon(weapon));
            gears.ForEach(gear => equipmentAbility?.EquipGear(gear));
            // 在初始化人形角色时由于武器和装备存在资源属性，需要在初始化后重新设置一次资源
            ResourceAbility.FillResource(false);
        }

        public void SetRace(HumanoidCharacterRace race)
        {
            HumanoidParameters.race = race;
            // 判断当前姿势是否是无武器姿势（即种族默认姿势），是则需要重新设置动画过渡库
            if (HumanoidParameters.pose == HumanoidCharacterPose.NoWeapon)
            {
                SetHumanoidTransitionLibrary(race, HumanoidParameters.pose);
            }
        }

        public void SetPose(HumanoidCharacterPose pose)
        {
            HumanoidParameters.pose = pose;
            SetHumanoidTransitionLibrary(HumanoidParameters.race, pose);
        }

        private void SetHumanoidTransitionLibrary(HumanoidCharacterRace race, HumanoidCharacterPose pose)
        {
            if (pose == HumanoidCharacterPose.NoWeapon)
            {
                // 如果是默认姿势就去找种族对应的动画过渡库
                HumanoidCharacterSingletonConfigSO.Instance.raceTransitionLibraryConfigurations.TryGetValue(race,
                    out var asset);
                if (!asset)
                {
                    throw new Exception($"The transition library asset of the race({race}) is not existed");
                }

                AnimationAbility?.SwitchTransitionLibrary(asset);
            }
            else
            {
                // 否则就直接找持有武器姿势对应的动画过渡库
                HumanoidCharacterSingletonConfigSO.Instance.poseTransitionLibraryConfigurations.TryGetValue(pose,
                    out var asset);
                if (!asset)
                {
                    throw new Exception($"The transition library asset of the pose({pose}) is not existed");
                }

                AnimationAbility?.SwitchTransitionLibrary(asset);
            }
        }
    }
}