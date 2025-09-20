using System;
using Character.Ability;
using Character.Ability.Appearance;
using Character.Data.Extension;
using Humanoid.Data;
using Humanoid.Model;
using Humanoid.Model.Data;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace Humanoid.Ability
{
    public class HumanoidAppearanceAbility : CharacterAppearanceAbility
    {
        private new HumanoidCharacterObject Owner => base.Owner as HumanoidCharacterObject;
        
        [Title("模型配置")] [SerializeField] private GameObject model;
        [SerializeField] private Material material;

        private HumanoidModelLoader _modelLoader;

        protected override void OnInit()
        {
            base.OnInit();
            _modelLoader = new HumanoidModelLoader(model, material);
        }

        protected override void OnDispose()
        {
            base.OnDispose();
            _modelLoader.Destroy();
        }

        public override void SetAppearance(object[] payload)
        {
            if (payload == null || payload.Length == 0)
            {
                return;
            }

            // 如果参数传入AppearanceData，就直接设置外观
            if (payload[0] is HumanoidAppearanceData appearanceData)
            {
                SetAppearance(appearanceData);
                return;
            }

            // 如果参数传入string，就根据传入id查找对应的外观数据并设置外观
            if (payload[0] is string appearanceBodyId)
            {
                var modelInfoContainer =
                    GameApplication.Instance.ExcelBinaryManager.GetContainer<HumanoidModelInfoContainer>();
                var appearanceBodyInfoContainer = GameApplication.Instance.ExcelBinaryManager
                    .GetContainer<HumanoidAppearanceBodyInfoContainer>();
                SetAppearance(HumanoidAppearanceData.JustBody(appearanceBodyInfoContainer
                    .Data[int.Parse(appearanceBodyId)]
                    .ToBodyAppearance(modelInfoContainer)));
            }
        }

        public void SetAppearance(HumanoidAppearanceData appearanceData)
        {
            Owner.HumanoidParameters.appearance = appearanceData;
            Owner.HumanoidParameters.appearance.SynchronizeAppearanceModel(_modelLoader);
        }

        public void PutOnGear(HumanoidAppearanceGearPart gearPart, HumanoidGearAppearance gearAppearance)
        {
            switch (gearPart)
            {
                case HumanoidAppearanceGearPart.Head:
                    Owner.HumanoidParameters.appearance.HeadGear = gearAppearance;
                    break;
                case HumanoidAppearanceGearPart.Torso:
                    Owner.HumanoidParameters.appearance.TorsoGear = gearAppearance;
                    break;
                case HumanoidAppearanceGearPart.LeftArm:
                    Owner.HumanoidParameters.appearance.LeftArmGear = gearAppearance;
                    break;
                case HumanoidAppearanceGearPart.RightArm:
                    Owner.HumanoidParameters.appearance.RightArmGear = gearAppearance;
                    break;
                case HumanoidAppearanceGearPart.LeftLeg:
                    Owner.HumanoidParameters.appearance.LeftLegGear = gearAppearance;
                    break;
                case HumanoidAppearanceGearPart.RightLeg:
                    Owner.HumanoidParameters.appearance.RightLegGear = gearAppearance;
                    break;
            }

            Owner.HumanoidParameters.appearance.SynchronizeAppearanceModel(_modelLoader);
        }

        public void TakeOffGear(HumanoidAppearanceGearPart gearPart)
        {
            switch (gearPart)
            {
                case HumanoidAppearanceGearPart.Head:
                    Owner.HumanoidParameters.appearance.HeadGear = HumanoidGearAppearance.Empty;
                    break;
                case HumanoidAppearanceGearPart.Torso:
                    Owner.HumanoidParameters.appearance.TorsoGear = HumanoidGearAppearance.Empty;
                    break;
                case HumanoidAppearanceGearPart.LeftArm:
                    Owner.HumanoidParameters.appearance.LeftArmGear = HumanoidGearAppearance.Empty;
                    break;
                case HumanoidAppearanceGearPart.RightArm:
                    Owner.HumanoidParameters.appearance.RightArmGear = HumanoidGearAppearance.Empty;
                    break;
                case HumanoidAppearanceGearPart.LeftLeg:
                    Owner.HumanoidParameters.appearance.LeftLegGear = HumanoidGearAppearance.Empty;
                    break;
                case HumanoidAppearanceGearPart.RightLeg:
                    Owner.HumanoidParameters.appearance.RightLegGear = HumanoidGearAppearance.Empty;
                    break;
            }

            Owner.HumanoidParameters.appearance.SynchronizeAppearanceModel(_modelLoader);
        }
    }
}