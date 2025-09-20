using Humanoid.Model.Data;
using UnityEngine;

namespace Humanoid.Model.Extension
{
    public static class HumanoidModelRecordExtension
    {
        public static HumanoidModelRecord ToModelRecord(this HumanoidModelInfoData modelInfoData)
        {
            return new HumanoidModelRecord
            {
                Type = modelInfoData.Type switch
                {
                    "body" => HumanoidModelType.Body,
                    "gear" => HumanoidModelType.Gear,
                },
                Part = modelInfoData.Part,
                GenderRestriction = modelInfoData.GenderRestriction switch
                {
                    "none" => HumanoidModelGenderRestriction.None,
                    "female only" => HumanoidModelGenderRestriction.FemaleOnly,
                    "male only" => HumanoidModelGenderRestriction.MaleOnly,
                },
                Name = modelInfoData.Name,
            };
        }

        public static HumanoidModelInfoData ToModelInfoData(this HumanoidModelRecord modelRecord, int id)
        {
            return new HumanoidModelInfoData
            {
                Id = id,
                Type = modelRecord.Type switch
                {
                    HumanoidModelType.Body => "body",
                    HumanoidModelType.Gear => "gear",
                },
                Part = modelRecord.Part,
                GenderRestriction = modelRecord.GenderRestriction switch
                {
                    HumanoidModelGenderRestriction.None => "none",
                    HumanoidModelGenderRestriction.FemaleOnly => "female only",
                    HumanoidModelGenderRestriction.MaleOnly => "male only",
                },
                Name = modelRecord.Name,
            };
        }
    }
}