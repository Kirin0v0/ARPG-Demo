using Character;
using Character.Ability;
using Character.Data;
using Character.Data.Extension;
using Humanoid.Data;
using Humanoid.Model.Data;
using Package.Data;
using Package.Data.Extension;
using Package.Runtime;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

namespace Humanoid.Ability
{
    public class HumanoidEquipmentAbility : BaseCharacterOptionalAbility
    {
        private new HumanoidCharacterObject Owner => base.Owner as HumanoidCharacterObject;
        
        [Title("装备栏")]
        [ShowInInspector, ReadOnly]
        public PackageGroup HeadGear { get; private set; }

        [ShowInInspector, ReadOnly] public PackageGroup TorsoGear { get; private set; }
        [ShowInInspector, ReadOnly] public PackageGroup LeftArmGear { get; private set; }
        [ShowInInspector, ReadOnly] public PackageGroup RightArmGear { get; private set; }
        [ShowInInspector, ReadOnly] public PackageGroup LeftLegGear { get; private set; }
        [ShowInInspector, ReadOnly] public PackageGroup RightLegGear { get; private set; }

        public event System.Action<HumanoidCharacterObject> OnGearsChanged;

        public bool EquipGear(PackageGroup packageGroup)
        {
            if (packageGroup.Data is not PackageGearData packageGearData)
            {
                return false;
            }
            
            if (!packageGearData.MatchRaceRestriction(Owner.HumanoidParameters.race))
            {
                return false;
            }

            switch (packageGearData.Part)
            {
                case HumanoidAppearanceGearPart.Head:
                {
                    HeadGear = packageGroup;
                }
                    break;
                case HumanoidAppearanceGearPart.Torso:
                {
                    TorsoGear = packageGroup;
                }
                    break;
                case HumanoidAppearanceGearPart.LeftArm:
                {
                    LeftArmGear = packageGroup;
                }
                    break;
                case HumanoidAppearanceGearPart.RightArm:
                {
                    RightArmGear = packageGroup;
                }
                    break;
                case HumanoidAppearanceGearPart.LeftLeg:
                {
                    LeftLegGear = packageGroup;
                }
                    break;
                case HumanoidAppearanceGearPart.RightLeg:
                {
                    RightLegGear = packageGroup;
                }
                    break;
            }

            // 设置装备外观
            Owner.AppearanceAbility?.PutOnGear(packageGearData.Part,
                packageGearData.Appearance.ToGearAppearance(
                    GameApplication.Instance.ExcelBinaryManager.GetContainer<HumanoidModelInfoContainer>())
            );
            // 重置装备属性
            ResetGearProperty();
            OnGearsChanged?.Invoke(Owner);
            return true;
        }

        public bool UnequipGear(PackageGroup packageGroup)
        {
            var result = false;
            if (HeadGear == packageGroup)
            {
                HeadGear = null;
                Owner.AppearanceAbility?.TakeOffGear(HumanoidAppearanceGearPart.Head);
                result = true;
            }

            if (TorsoGear == packageGroup)
            {
                TorsoGear = null;
                Owner.AppearanceAbility?.TakeOffGear(HumanoidAppearanceGearPart.Torso);
                result = true;
            }

            if (LeftArmGear == packageGroup)
            {
                LeftArmGear = null;
                Owner.AppearanceAbility?.TakeOffGear(HumanoidAppearanceGearPart.LeftArm);
                result = true;
            }

            if (RightArmGear == packageGroup)
            {
                RightArmGear = null;
                Owner.AppearanceAbility?.TakeOffGear(HumanoidAppearanceGearPart.RightArm);
                result = true;
            }

            if (LeftLegGear == packageGroup)
            {
                LeftLegGear = null;
                Owner.AppearanceAbility?.TakeOffGear(HumanoidAppearanceGearPart.LeftLeg);
                result = true;
            }

            if (RightLegGear == packageGroup)
            {
                RightLegGear = null;
                Owner.AppearanceAbility?.TakeOffGear(HumanoidAppearanceGearPart.RightLeg);
                result = true;
            }

            if (result)
            {
                ResetGearProperty();
                OnGearsChanged?.Invoke(Owner);
            }

            return result;
        }

        private void ResetGearProperty()
        {
            var property = CharacterProperty.Zero;
            if (HeadGear != null)
            {
                property += CalculateGearProperty(HeadGear);
            }

            if (TorsoGear != null)
            {
                property += CalculateGearProperty(TorsoGear);
            }

            if (LeftArmGear != null)
            {
                property += CalculateGearProperty(LeftArmGear);
            }

            if (RightArmGear != null)
            {
                property += CalculateGearProperty(RightArmGear);
            }

            if (LeftLegGear != null)
            {
                property += CalculateGearProperty(LeftLegGear);
            }

            if (RightLegGear != null)
            {
                property += CalculateGearProperty(RightLegGear);
            }

            Owner.HumanoidParameters.gearProperty = property;
            Owner.PropertyAbility.CheckProperty();

            return;

            CharacterProperty CalculateGearProperty(PackageGroup packageGroup)
            {
                if (packageGroup.Data is not PackageGearData packageGearData)
                {
                    return CharacterProperty.Zero;
                }
                
                return new CharacterProperty
                {
                    maxHp = packageGearData.MaxHp,
                    maxMp = packageGearData.MaxMp,
                    stamina = packageGearData.Stamina,
                    strength = packageGearData.Strength,
                    magic = packageGearData.Magic,
                    reaction = packageGearData.Reaction,
                    luck = packageGearData.Luck,
                };
            }
        }
    }
}