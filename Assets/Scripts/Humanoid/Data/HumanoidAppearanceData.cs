using System;
using System.Collections.Generic;
using Character.Data.Extension;
using Humanoid.Model.Data;
using UnityEngine;
using UnityEngine.Serialization;

namespace Humanoid.Data
{
    [Flags]
    public enum HumanoidAppearanceRace
    {
        None = 0,
        HumanMale = 1 << 0,
        HumanFemale = 1 << 1,
    }

    public struct HumanoidAppearanceData
    {
        public HumanoidBodyAppearance Body;
        public HumanoidGearAppearance HeadGear;
        public HumanoidGearAppearance TorsoGear;
        public HumanoidGearAppearance LeftArmGear;
        public HumanoidGearAppearance RightArmGear;
        public HumanoidGearAppearance LeftLegGear;
        public HumanoidGearAppearance RightLegGear;

        public static HumanoidAppearanceData Empty = new HumanoidAppearanceData
        {
            Body = HumanoidBodyAppearance.Empty,
            HeadGear = HumanoidGearAppearance.Empty,
            TorsoGear = HumanoidGearAppearance.Empty,
            LeftArmGear = HumanoidGearAppearance.Empty,
            RightArmGear = HumanoidGearAppearance.Empty,
            LeftLegGear = HumanoidGearAppearance.Empty,
            RightLegGear = HumanoidGearAppearance.Empty,
        };

        public static HumanoidAppearanceData JustBody(HumanoidBodyAppearance body)
        {
            var data = Empty;
            data.Body = body;
            return data;
        }
    }

    public struct HumanoidAppearanceModel
    {
        public int Id;
        public string Part;
        public HumanoidModelGenderRestriction GenderRestriction;
        public string Name;
    }

    public struct HumanoidAppearanceColor
    {
        public Color SkinColor;
        public Color HairColor;
        public Color StubbleColor;
        public Color EyesColor;
        public Color ScarColor;
        public Color BodyArtColor;
        public Color PrimaryColor;
        public Color SecondaryColor;
        public Color MetalPrimaryColor;
        public Color MetalSecondaryColor;
        public Color MetalDarkColor;
        public Color LeatherPrimaryColor;
        public Color LeatherSecondaryColor;

        public static HumanoidAppearanceColor DefaultBodyColor =
            HumanoidModelColor.DefaultBodyColor.ToAppearanceColor();

        public static HumanoidAppearanceColor DefaultGearColor =
            HumanoidModelColor.DefaultGearColor.ToAppearanceColor();

        public HumanoidAppearanceColor Clone(
            Color? skinColor = null,
            Color? hairColor = null,
            Color? stubbleColor = null,
            Color? eyesColor = null,
            Color? scarColor = null,
            Color? bodyArtColor = null,
            Color? primaryColor = null,
            Color? secondaryColor = null,
            Color? metalPrimaryColor = null,
            Color? metalSecondaryColor = null,
            Color? metalDarkColor = null,
            Color? leatherPrimaryColor = null,
            Color? leatherSecondaryColor = null
        )
        {
            return new HumanoidAppearanceColor
            {
                SkinColor = skinColor ?? SkinColor,
                HairColor = hairColor ?? HairColor,
                StubbleColor = stubbleColor ?? StubbleColor,
                EyesColor = eyesColor ?? EyesColor,
                ScarColor = scarColor ?? ScarColor,
                BodyArtColor = bodyArtColor ?? BodyArtColor,
                PrimaryColor = primaryColor ?? PrimaryColor,
                SecondaryColor = secondaryColor ?? SecondaryColor,
                MetalPrimaryColor = metalPrimaryColor ?? MetalPrimaryColor,
                MetalSecondaryColor = metalSecondaryColor ?? MetalSecondaryColor,
                MetalDarkColor = metalDarkColor ?? MetalDarkColor,
                LeatherPrimaryColor = leatherPrimaryColor ?? LeatherPrimaryColor,
                LeatherSecondaryColor = leatherSecondaryColor ?? LeatherSecondaryColor,
            };
        }
    }

    public struct HumanoidBodyAppearance
    {
        public List<HumanoidAppearanceModel> Models;
        public HumanoidAppearanceColor Color;
        
        public static HumanoidBodyAppearance Empty = new HumanoidBodyAppearance
        {
            Models = new List<HumanoidAppearanceModel>(),
            Color = HumanoidAppearanceColor.DefaultBodyColor,
        };
    }

    public struct HumanoidGearAppearance
    {
        public List<HumanoidAppearanceModel> Models;
        public HumanoidAppearanceColor Color;

        public static HumanoidGearAppearance Empty = new HumanoidGearAppearance
        {
            Models = new List<HumanoidAppearanceModel>(),
            Color = HumanoidAppearanceColor.DefaultGearColor,
        };
    }

    public enum HumanoidAppearanceGearPart
    {
        Head,
        Torso,
        LeftArm,
        RightArm,
        LeftLeg,
        RightLeg,
    }

    public enum HumanoidAppearanceWeaponType
    {
        Fantasy,
        Samurai,
    }
}