using System.Collections.Generic;
using UnityEngine;

namespace Humanoid.Model.Data
{
    public enum HumanoidModelType
    {
        Body,
        Gear,
    }

    public enum HumanoidModelGenderRestriction
    {
        None,
        FemaleOnly,
        MaleOnly,
    }

    public record HumanoidModelClassification
    {
        public HumanoidModelType Type;
        public string Part;
    }

    public record HumanoidModelGroup
    {
        public HumanoidModelGenderRestriction GenderRestriction;
        public List<GameObject> Models;
    }

    public record HumanoidModelIdentity
    {
        public HumanoidModelType Type;
        public string Part;
        public HumanoidModelGenderRestriction GenderRestriction;
    }
    
    public record HumanoidModelRecord
    {
        public HumanoidModelType Type;
        public string Part;
        public HumanoidModelGenderRestriction GenderRestriction;
        public string Name;
    }
}