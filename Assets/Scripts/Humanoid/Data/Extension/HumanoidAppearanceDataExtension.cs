using System;
using System.Collections.Generic;
using System.Linq;
using Framework.DataStructure;
using Humanoid;
using Humanoid.Data;
using Humanoid.Model;
using Humanoid.Model.Data;
using Humanoid.Model.Extension;
using Package.Data;
using Sirenix.Utilities;
using UnityEngine;

namespace Character.Data.Extension
{
    public static class HumanoidAppearanceDataExtension
    {
        public static void SynchronizeAppearanceModel(this HumanoidAppearanceData data, HumanoidModelLoader loader)
        {
            loader.HideAllModels();
            data.Body.Models.ForEach(model => loader.ShowModel(HumanoidModelType.Body, model.Part,
                model.GenderRestriction, model.Name, data.Body.Color.ToModelColor()));
            ShowGearModel(data.HeadGear);
            ShowGearModel(data.TorsoGear);
            ShowGearModel(data.LeftArmGear);
            ShowGearModel(data.RightArmGear);
            ShowGearModel(data.LeftLegGear);
            ShowGearModel(data.RightLegGear);
            return;

            void ShowGearModel(HumanoidGearAppearance gearAppearance)
            {
                gearAppearance.Models.ForEach(model => loader.ShowModel(
                    HumanoidModelType.Gear,
                    model.Part,
                    model.GenderRestriction,
                    model.Name,
                    GearColorToModelColor(gearAppearance.Color)
                ));
            }

            HumanoidModelColor GearColorToModelColor(HumanoidAppearanceColor gearColor)
            {
                return new HumanoidModelColor
                {
                    SkinColor = data.Body.Color.SkinColor,
                    HairColor = data.Body.Color.HairColor,
                    StubbleColor = data.Body.Color.StubbleColor,
                    EyesColor = data.Body.Color.EyesColor,
                    ScarColor = data.Body.Color.ScarColor,
                    BodyArtColor = data.Body.Color.BodyArtColor,
                    PrimaryColor = gearColor.PrimaryColor,
                    SecondaryColor = gearColor.SecondaryColor,
                    MetalPrimaryColor = gearColor.MetalPrimaryColor,
                    MetalSecondaryColor = gearColor.MetalSecondaryColor,
                    MetalDarkColor = gearColor.MetalDarkColor,
                    LeatherPrimaryColor = gearColor.LeatherPrimaryColor,
                    LeatherSecondaryColor = gearColor.LeatherSecondaryColor,
                };
            }
        }

        public static HumanoidAppearanceArchiveData ToArchiveData(this HumanoidAppearanceData data)
        {
            return new HumanoidAppearanceArchiveData
            {
                body = data.Body.ToArchiveData(),
                headGear = data.HeadGear.ToArchiveData(),
                torsoGear = data.TorsoGear.ToArchiveData(),
                leftArmGear = data.LeftArmGear.ToArchiveData(),
                rightArmGear = data.RightArmGear.ToArchiveData(),
                leftLegGear = data.LeftLegGear.ToArchiveData(),
                rightLegGear = data.RightLegGear.ToArchiveData(),
            };
        }

        public static HumanoidBodyAppearanceArchiveData ToArchiveData(this HumanoidBodyAppearance data)
        {
            return new HumanoidBodyAppearanceArchiveData
            {
                models = data.Models.Select(model => model.Id).ToList(),
                color = data.Color.ToArchiveData(),
            };
        }

        public static HumanoidGearAppearanceArchiveData ToArchiveData(this HumanoidGearAppearance data)
        {
            return new HumanoidGearAppearanceArchiveData
            {
                models = data.Models.Select(model => model.Id).ToList(),
                color = data.Color.ToArchiveData(),
            };
        }

        public static HumanoidAppearanceData ToAppearanceData(this HumanoidAppearanceArchiveData data,
            HumanoidModelInfoContainer modelInfoContainer)
        {
            return new HumanoidAppearanceData
            {
                Body = new HumanoidBodyAppearance
                {
                    Models = GetModels(data.body.models),
                    Color = data.body.color.ToAppearanceColor(),
                },
                HeadGear = new HumanoidGearAppearance
                {
                    Models = GetModels(data.headGear.models),
                    Color = data.headGear.color.ToAppearanceColor(),
                },
                TorsoGear = new HumanoidGearAppearance
                {
                    Models = GetModels(data.torsoGear.models),
                    Color = data.torsoGear.color.ToAppearanceColor(),
                },
                LeftArmGear = new HumanoidGearAppearance
                {
                    Models = GetModels(data.leftArmGear.models),
                    Color = data.leftArmGear.color.ToAppearanceColor(),
                },
                RightArmGear = new HumanoidGearAppearance
                {
                    Models = GetModels(data.rightArmGear.models),
                    Color = data.rightArmGear.color.ToAppearanceColor(),
                },
                LeftLegGear = new HumanoidGearAppearance
                {
                    Models = GetModels(data.leftLegGear.models),
                    Color = data.leftLegGear.color.ToAppearanceColor(),
                },
                RightLegGear = new HumanoidGearAppearance
                {
                    Models = GetModels(data.rightLegGear.models),
                    Color = data.rightLegGear.color.ToAppearanceColor(),
                },
            };

            List<HumanoidAppearanceModel> GetModels(List<int> modelIds)
            {
                return modelIds.Where(modelId => modelInfoContainer.Data.ContainsKey(modelId))
                    .Select(modelId => modelInfoContainer.Data[modelId])
                    .Select(modelInfo =>
                    {
                        var modelRecord = modelInfo.ToModelRecord();
                        return new HumanoidAppearanceModel
                        {
                            Id = modelInfo.Id,
                            Part = modelRecord.Part,
                            GenderRestriction = modelRecord.GenderRestriction,
                            Name = modelRecord.Name
                        };
                    }).ToList();
            }
        }

        public static HumanoidAppearanceColorArchiveData ToArchiveData(this HumanoidAppearanceColor appearanceColor)
        {
            return new HumanoidAppearanceColorArchiveData
            {
                skinColor = new SerializableColor(appearanceColor.SkinColor),
                hairColor = new SerializableColor(appearanceColor.HairColor),
                stubbleColor = new SerializableColor(appearanceColor.StubbleColor),
                eyesColor = new SerializableColor(appearanceColor.EyesColor),
                scarColor = new SerializableColor(appearanceColor.ScarColor),
                bodyArtColor = new SerializableColor(appearanceColor.BodyArtColor),
                primaryColor = new SerializableColor(appearanceColor.PrimaryColor),
                secondaryColor = new SerializableColor(appearanceColor.SecondaryColor),
                metalPrimaryColor = new SerializableColor(appearanceColor.MetalPrimaryColor),
                metalSecondaryColor = new SerializableColor(appearanceColor.MetalSecondaryColor),
                metalDarkColor = new SerializableColor(appearanceColor.MetalDarkColor),
                leatherPrimaryColor = new SerializableColor(appearanceColor.LeatherPrimaryColor),
                leatherSecondaryColor = new SerializableColor(appearanceColor.LeatherSecondaryColor),
            };
        }

        public static HumanoidAppearanceColor ToAppearanceColor(this HumanoidAppearanceColorArchiveData archiveData)
        {
            return new HumanoidAppearanceColor
            {
                SkinColor = archiveData.skinColor.ToColor(),
                HairColor = archiveData.hairColor.ToColor(),
                StubbleColor = archiveData.stubbleColor.ToColor(),
                EyesColor = archiveData.eyesColor.ToColor(),
                ScarColor = archiveData.scarColor.ToColor(),
                BodyArtColor = archiveData.bodyArtColor.ToColor(),
                PrimaryColor = archiveData.primaryColor.ToColor(),
                SecondaryColor = archiveData.secondaryColor.ToColor(),
                MetalPrimaryColor = archiveData.metalPrimaryColor.ToColor(),
                MetalSecondaryColor = archiveData.metalSecondaryColor.ToColor(),
                MetalDarkColor = archiveData.metalDarkColor.ToColor(),
                LeatherPrimaryColor = archiveData.leatherPrimaryColor.ToColor(),
                LeatherSecondaryColor = archiveData.leatherSecondaryColor.ToColor(),
            };
        }

        public static HumanoidModelColor ToModelColor(this HumanoidAppearanceColor appearanceColor)
        {
            return new HumanoidModelColor
            {
                SkinColor = appearanceColor.SkinColor,
                HairColor = appearanceColor.HairColor,
                StubbleColor = appearanceColor.StubbleColor,
                EyesColor = appearanceColor.EyesColor,
                ScarColor = appearanceColor.ScarColor,
                BodyArtColor = appearanceColor.BodyArtColor,
                PrimaryColor = appearanceColor.PrimaryColor,
                SecondaryColor = appearanceColor.SecondaryColor,
                MetalPrimaryColor = appearanceColor.MetalPrimaryColor,
                MetalSecondaryColor = appearanceColor.MetalSecondaryColor,
                MetalDarkColor = appearanceColor.MetalDarkColor,
                LeatherPrimaryColor = appearanceColor.LeatherPrimaryColor,
                LeatherSecondaryColor = appearanceColor.LeatherSecondaryColor
            };
        }

        public static HumanoidAppearanceColor ToAppearanceColor(this HumanoidModelColor modelColor)
        {
            return new HumanoidAppearanceColor
            {
                SkinColor = modelColor.SkinColor,
                HairColor = modelColor.HairColor,
                StubbleColor = modelColor.StubbleColor,
                EyesColor = modelColor.EyesColor,
                ScarColor = modelColor.ScarColor,
                BodyArtColor = modelColor.BodyArtColor,
                PrimaryColor = modelColor.PrimaryColor,
                SecondaryColor = modelColor.SecondaryColor,
                MetalPrimaryColor = modelColor.MetalPrimaryColor,
                MetalSecondaryColor = modelColor.MetalSecondaryColor,
                MetalDarkColor = modelColor.MetalDarkColor,
                LeatherPrimaryColor = modelColor.LeatherPrimaryColor,
                LeatherSecondaryColor = modelColor.LeatherSecondaryColor
            };
        }

        public static HumanoidBodyAppearance ToBodyAppearance(
            this HumanoidAppearanceBodyInfoData data,
            HumanoidModelInfoContainer modelInfoContainer
        )
        {
            var modelIds = data.Models.Split(",");
            return new HumanoidBodyAppearance
            {
                Models = modelIds.Where(modelId => modelInfoContainer.Data.ContainsKey(int.Parse(modelId)))
                    .Select(modelId => modelInfoContainer.Data[int.Parse(modelId)])
                    .Select(modelInfo =>
                    {
                        var modelRecord = modelInfo.ToModelRecord();
                        return new HumanoidAppearanceModel
                        {
                            Id = modelInfo.Id,
                            Part = modelRecord.Part,
                            GenderRestriction = modelRecord.GenderRestriction,
                            Name = modelRecord.Name
                        };
                    }).ToList(),
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

        public static HumanoidGearAppearance ToGearAppearance(
            this HumanoidAppearanceGearInfoData data,
            HumanoidModelInfoContainer modelInfoContainer
        )
        {
            var modelIds = data.Models.Split(",");
            return new HumanoidGearAppearance
            {
                Models = modelIds.Where(modelId => modelInfoContainer.Data.ContainsKey(int.Parse(modelId)))
                    .Select(modelId => modelInfoContainer.Data[int.Parse(modelId)])
                    .Select(modelInfo =>
                    {
                        var modelRecord = modelInfo.ToModelRecord();
                        return new HumanoidAppearanceModel
                        {
                            Id = modelInfo.Id,
                            Part = modelRecord.Part,
                            GenderRestriction = modelRecord.GenderRestriction,
                            Name = modelRecord.Name
                        };
                    }).ToList(),
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

        public static HumanoidAppearanceColor GetFantasyAppearanceColor(this HumanoidAppearanceWeaponInfoData data)
        {
            if (data.Type.ToWeaponAppearanceType() != HumanoidAppearanceWeaponType.Fantasy)
            {
                return HumanoidAppearanceColor.DefaultGearColor;
            }
            
            var colors = data.Payload.Split(",");
            var primaryColor = colors.Length > 0 ? colors[0].ToColor() : Color.clear;
            var secondaryColor = colors.Length > 1 ? colors[1].ToColor() : Color.clear;
            var metalPrimaryColor = colors.Length > 2 ? colors[2].ToColor() : Color.clear;
            var metalSecondaryColor = colors.Length > 3 ? colors[3].ToColor() : Color.clear;
            var metalDarkColor = colors.Length > 4 ? colors[4].ToColor() : Color.clear;
            var leatherPrimaryColor = colors.Length > 5 ? colors[5].ToColor() : Color.clear;
            var leatherSecondaryColor = colors.Length > 6 ? colors[6].ToColor() : Color.clear;

            return new HumanoidAppearanceColor
            {
                SkinColor = Color.clear,
                HairColor = Color.clear,
                StubbleColor = Color.clear,
                EyesColor = Color.clear,
                ScarColor = Color.clear,
                BodyArtColor = Color.clear,
                PrimaryColor = primaryColor,
                SecondaryColor = secondaryColor,
                MetalPrimaryColor = metalPrimaryColor,
                MetalSecondaryColor = metalSecondaryColor,
                MetalDarkColor = metalDarkColor,
                LeatherPrimaryColor = leatherPrimaryColor,
                LeatherSecondaryColor = leatherSecondaryColor
            };
        }

        public static HumanoidAppearanceGearPart GetGearPart(this HumanoidAppearanceGearInfoData data)
        {
            return data.Part.ToPart();
        }

        public static string ToRacesString(this HumanoidAppearanceRace race)
        {
            var races = new List<string>();
            if ((race & HumanoidAppearanceRace.HumanMale) != 0)
            {
                races.Add("human male");
            }

            if ((race & HumanoidAppearanceRace.HumanFemale) != 0)
            {
                races.Add("human female");
            }

            return String.Join(",", races);
        }

        public static string GetString(this HumanoidAppearanceGearPart gearPart)
        {
            return gearPart switch
            {
                HumanoidAppearanceGearPart.Head => "head",
                HumanoidAppearanceGearPart.Torso => "torso",
                HumanoidAppearanceGearPart.LeftArm => "left arm",
                HumanoidAppearanceGearPart.RightArm => "right arm",
                HumanoidAppearanceGearPart.LeftLeg => "left leg",
                HumanoidAppearanceGearPart.RightLeg => "right leg",
            };
        }

        public static string GetString(this HumanoidAppearanceWeaponType weaponType)
        {
            return weaponType switch
            {
                HumanoidAppearanceWeaponType.Fantasy => "fantasy",
                HumanoidAppearanceWeaponType.Samurai => "samurai",
                _ => "fantasy",
            };
        }

        public static HumanoidAppearanceWeaponType ToWeaponAppearanceType(this string type)
        {
            return type switch
            {
                "fantasy" => HumanoidAppearanceWeaponType.Fantasy,
                "samurai" => HumanoidAppearanceWeaponType.Samurai,
            };
        }

        public static HumanoidAppearanceRace ToAppearanceRace(this HumanoidCharacterRace characterRace)
        {
            return characterRace switch
            {
                HumanoidCharacterRace.HumanMale => HumanoidAppearanceRace.HumanMale,
                HumanoidCharacterRace.HumanFemale => HumanoidAppearanceRace.HumanFemale,
                _ => HumanoidAppearanceRace.None
            };
        }

        public static bool MatchGenderRestriction(this HumanoidAppearanceRace races,
            HumanoidModelGenderRestriction genderRestriction)
        {
            if (races == HumanoidAppearanceRace.None)
            {
                return false;
            }

            if (races.HasFlag(HumanoidAppearanceRace.HumanMale) &&
                genderRestriction is HumanoidModelGenderRestriction.FemaleOnly)
            {
                return false;
            }

            if (races.HasFlag(HumanoidAppearanceRace.HumanFemale) &&
                genderRestriction is HumanoidModelGenderRestriction.MaleOnly)
            {
                return false;
            }

            return true;
        }

        public static bool MatchRestriction(this HumanoidAppearanceRace appearanceRaces,
            HumanoidCharacterRace race)
        {
            if (appearanceRaces.HasFlag(HumanoidAppearanceRace.HumanMale) && race == HumanoidCharacterRace.HumanMale)
            {
                return true;
            }

            if (appearanceRaces.HasFlag(HumanoidAppearanceRace.HumanFemale) &&
                race == HumanoidCharacterRace.HumanFemale)
            {
                return true;
            }

            return false;
        }

        public static HumanoidAppearanceRace GetRace(this HumanoidAppearanceGearInfoData gearInfoData)
        {
            return gearInfoData.Races.ToRace();
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

        private static HumanoidAppearanceGearPart ToPart(this string part)
        {
            return part switch
            {
                "head" => HumanoidAppearanceGearPart.Head,
                "torso" => HumanoidAppearanceGearPart.Torso,
                "left arm" => HumanoidAppearanceGearPart.LeftArm,
                "right arm" => HumanoidAppearanceGearPart.RightArm,
                "left leg" => HumanoidAppearanceGearPart.LeftLeg,
                "right leg" => HumanoidAppearanceGearPart.RightLeg,
            };
        }
        
        public static HumanoidAppearanceGearPart ToGearPart(this string part)
        {
            return part switch
            {
                "head" => HumanoidAppearanceGearPart.Head,
                "torso" => HumanoidAppearanceGearPart.Torso,
                "left arm" => HumanoidAppearanceGearPart.LeftArm,
                "right arm" => HumanoidAppearanceGearPart.RightArm,
                "left leg" => HumanoidAppearanceGearPart.LeftLeg,
                "right leg" => HumanoidAppearanceGearPart.RightLeg,
            };
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