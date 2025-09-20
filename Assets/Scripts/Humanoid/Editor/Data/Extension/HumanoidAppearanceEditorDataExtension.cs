using System;
using System.Linq;
using Character.Data.Extension;
using Humanoid.Data;
using Humanoid.Model.Data;
using Sirenix.Utilities;
using UnityEngine;

namespace Humanoid.Editor.Data.Extension
{
    public static class HumanoidAppearanceEditorDataExtension
    {
        public static HumanoidAppearanceBodyUIData ToUIData(
            this HumanoidAppearanceBodyInfoData data,
            HumanoidModelInfoContainer modelInfoContainer
        )
        {
            var modelIds = data.Models.Split(",");
            return new HumanoidAppearanceBodyUIData
            {
                Id = data.Id,
                Races = data.Races.ToRace(),
                Models = modelIds.Where(modelId => modelInfoContainer.Data.ContainsKey(int.Parse(modelId)))
                    .Select(modelId => modelInfoContainer.Data[int.Parse(modelId)]).ToList(),
                Color = new HumanoidAppearanceColor
                {
                    SkinColor = data.SkinColor.ToColor(),
                    HairColor = data.HairColor.ToColor(),
                    StubbleColor = data.StubbleColor.ToColor(),
                    EyesColor = data.EyesColor.ToColor(),
                    ScarColor = data.ScarColor.ToColor(),
                    BodyArtColor = data.BodyArtColor.ToColor(),
                    PrimaryColor = data.PrimaryColor.ToColor(),
                    SecondaryColor = data.SecondaryColor.ToColor(),
                    MetalPrimaryColor = data.MetalPrimaryColor.ToColor(),
                    MetalSecondaryColor = data.MetalSecondaryColor.ToColor(),
                    MetalDarkColor = data.MetalDarkColor.ToColor(),
                    LeatherPrimaryColor = data.LeatherPrimaryColor.ToColor(),
                    LeatherSecondaryColor = data.LeatherSecondaryColor.ToColor(),
                },
            };
        }

        public static HumanoidAppearanceBodyInfoData ToInfoData(this HumanoidAppearanceBodyUIData data)
        {
            return new HumanoidAppearanceBodyInfoData
            {
                Id = data.Id,
                Races = data.Races.ToRacesString(),
                Models = String.Join(",", data.Models.Select(model => model.Id.ToString())),
                SkinColor = data.Color.SkinColor.ToColorString(),
                HairColor = data.Color.HairColor.ToColorString(),
                StubbleColor = data.Color.StubbleColor.ToColorString(),
                EyesColor = data.Color.EyesColor.ToColorString(),
                ScarColor = data.Color.ScarColor.ToColorString(),
                BodyArtColor = data.Color.BodyArtColor.ToColorString(),
                PrimaryColor = data.Color.PrimaryColor.ToColorString(),
                SecondaryColor = data.Color.SecondaryColor.ToColorString(),
                MetalPrimaryColor = data.Color.MetalPrimaryColor.ToColorString(),
                MetalSecondaryColor = data.Color.MetalSecondaryColor.ToColorString(),
                MetalDarkColor = data.Color.MetalDarkColor.ToColorString(),
                LeatherPrimaryColor = data.Color.LeatherPrimaryColor.ToColorString(),
                LeatherSecondaryColor = data.Color.LeatherSecondaryColor.ToColorString(),
            };
        }

        public static HumanoidAppearanceGearUIData ToUIData(
            this HumanoidAppearanceGearInfoData data,
            HumanoidModelInfoContainer modelInfoContainer
        )
        {
            var modelIds = data.Models.Split(",");
            return new HumanoidAppearanceGearUIData
            {
                Id = data.Id,
                Races = data.Races.ToRace(),
                Models = modelIds.Where(modelId => modelInfoContainer.Data.ContainsKey(int.Parse(modelId)))
                    .Select(modelId => modelInfoContainer.Data[int.Parse(modelId)]).ToList(),
                Part = data.Part.ToGearPart(),
                Mark = data.Mark,
                Color = new HumanoidAppearanceColor
                {
                    SkinColor = Color.clear,
                    HairColor = Color.clear,
                    StubbleColor = Color.clear,
                    EyesColor = Color.clear,
                    ScarColor = Color.clear,
                    BodyArtColor = Color.clear,
                    PrimaryColor = data.PrimaryColor.ToColor(),
                    SecondaryColor = data.SecondaryColor.ToColor(),
                    MetalPrimaryColor = data.MetalPrimaryColor.ToColor(),
                    MetalSecondaryColor = data.MetalSecondaryColor.ToColor(),
                    MetalDarkColor = data.MetalDarkColor.ToColor(),
                    LeatherPrimaryColor = data.LeatherPrimaryColor.ToColor(),
                    LeatherSecondaryColor = data.LeatherSecondaryColor.ToColor(),
                },
            };
        }

        public static HumanoidAppearanceGearInfoData ToInfoData(this HumanoidAppearanceGearUIData data)
        {
            return new HumanoidAppearanceGearInfoData
            {
                Id = data.Id,
                Races = data.Races.ToRacesString(),
                Models = String.Join(",", data.Models.Select(model => model.Id.ToString())),
                Part = data.Part.GetString(),
                Mark = data.Mark,
                PrimaryColor = data.Color.PrimaryColor.ToColorString(),
                SecondaryColor = data.Color.SecondaryColor.ToColorString(),
                MetalPrimaryColor = data.Color.MetalPrimaryColor.ToColorString(),
                MetalSecondaryColor = data.Color.MetalSecondaryColor.ToColorString(),
                MetalDarkColor = data.Color.MetalDarkColor.ToColorString(),
                LeatherPrimaryColor = data.Color.LeatherPrimaryColor.ToColorString(),
                LeatherSecondaryColor = data.Color.LeatherSecondaryColor.ToColorString(),
            };
        }

        public static HumanoidAppearanceWeaponUIData ToUIData(this HumanoidAppearanceWeaponInfoData data)
        {
            var type = data.Type.ToWeaponAppearanceType();
            switch (type)
            {
                case HumanoidAppearanceWeaponType.Samurai:
                {
                    return new HumanoidAppearanceWeaponUIData
                    {
                        Id = data.Id,
                        Model = data.Model,
                        Type = HumanoidAppearanceWeaponType.Samurai,
                        Comment = data.Comment,
                        SamuraiTexture = data.Payload,
                    };
                }
                    break;
                case HumanoidAppearanceWeaponType.Fantasy:
                default:
                {
                    return new HumanoidAppearanceWeaponUIData
                    {
                        Id = data.Id,
                        Model = data.Model,
                        Type = HumanoidAppearanceWeaponType.Fantasy,
                        Comment = data.Comment,
                        FantasyColor = data.GetFantasyAppearanceColor(),
                    };
                }
                    break;
            }
        }

        public static HumanoidAppearanceWeaponInfoData ToInfoData(this HumanoidAppearanceWeaponUIData data)
        {
            return new HumanoidAppearanceWeaponInfoData
            {
                Id = data.Id,
                Model = data.Model,
                Type = data.Type.GetString(),
                Comment = data.Comment,
                Payload = data.Type switch
                {
                    HumanoidAppearanceWeaponType.Fantasy =>
                        $"{data.FantasyColor.PrimaryColor.ToColorString()},{data.FantasyColor.SecondaryColor.ToColorString()},{data.FantasyColor.MetalPrimaryColor.ToColorString()},{data.FantasyColor.MetalSecondaryColor.ToColorString()},{data.FantasyColor.MetalDarkColor.ToColorString()},{data.FantasyColor.LeatherPrimaryColor.ToColorString()},{data.FantasyColor.LeatherSecondaryColor.ToColorString()}",
                    HumanoidAppearanceWeaponType.Samurai => data.SamuraiTexture,
                    _ => ""
                },
            };
        }

        private static HumanoidAppearanceRace ToRace(this string races)
        {
            var result = HumanoidAppearanceRace.None;
            var raceArray = races.Split(",");
            raceArray.ForEach(race =>
            {
                if (race == "human male")
                {
                    result |= HumanoidAppearanceRace.HumanMale;
                }

                if (race == "human female")
                {
                    result |= HumanoidAppearanceRace.HumanFemale;
                }
            });
            return result;
        }

        private static Color ToColor(this string colorString)
        {
            if (ColorUtility.TryParseHtmlString(colorString, out var color))
            {
                return color;
            }

            return Color.clear;
        }

        private static string ToColorString(this Color color)
        {
            return "#" + ColorUtility.ToHtmlStringRGBA(color);
        }
    }
}