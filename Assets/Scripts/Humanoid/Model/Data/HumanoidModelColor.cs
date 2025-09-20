using UnityEngine;

namespace Humanoid.Model.Data
{
    public struct HumanoidModelColor
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
        
        public static HumanoidModelColor Empty = new HumanoidModelColor
        {
            SkinColor = Color.clear,
            HairColor = Color.clear,
            StubbleColor = Color.clear,
            EyesColor = Color.clear,
            ScarColor = Color.clear,
            BodyArtColor = Color.clear,
            PrimaryColor = Color.clear,
            SecondaryColor = Color.clear,
            MetalPrimaryColor = Color.clear,
            MetalSecondaryColor = Color.clear,
            MetalDarkColor = Color.clear,
            LeatherPrimaryColor = Color.clear,
            LeatherSecondaryColor = Color.clear,
        };

        public static HumanoidModelColor DefaultBodyColor = new HumanoidModelColor
        {
            SkinColor = Color.white,
            HairColor = Color.grey,
            StubbleColor = Color.grey,
            EyesColor = Color.black,
            ScarColor = Color.grey,
            BodyArtColor = Color.grey,
            PrimaryColor = Color.grey,
            SecondaryColor = Color.grey,
            MetalPrimaryColor = Color.grey,
            MetalSecondaryColor = Color.grey,
            MetalDarkColor = Color.grey,
            LeatherPrimaryColor = Color.grey,
            LeatherSecondaryColor = Color.grey,
        };

        public static HumanoidModelColor DefaultGearColor = new HumanoidModelColor
        {
            SkinColor = Color.clear,
            HairColor = Color.clear,
            StubbleColor = Color.clear,
            EyesColor = Color.clear,
            ScarColor = Color.clear,
            BodyArtColor = Color.clear,
            PrimaryColor = Color.grey,
            SecondaryColor = Color.grey,
            MetalPrimaryColor = Color.grey,
            MetalSecondaryColor = Color.grey,
            MetalDarkColor = Color.grey,
            LeatherPrimaryColor = Color.grey,
            LeatherSecondaryColor = Color.grey,
        };

        public static HumanoidModelColor DefaultWeaponColor = new HumanoidModelColor
        {
            SkinColor = Color.clear,
            HairColor = Color.clear,
            StubbleColor = Color.clear,
            EyesColor = Color.clear,
            ScarColor = Color.clear,
            BodyArtColor = Color.clear,
            PrimaryColor = Color.white,
            SecondaryColor = Color.white,
            MetalPrimaryColor = Color.white,
            MetalSecondaryColor = Color.white,
            MetalDarkColor = Color.white,
            LeatherPrimaryColor = Color.white,
            LeatherSecondaryColor = Color.white,
        };
    }
}