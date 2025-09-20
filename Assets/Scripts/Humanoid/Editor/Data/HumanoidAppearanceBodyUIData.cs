using System.Collections.Generic;
using Humanoid.Data;
using Humanoid.Model.Data;
using UnityEngine;

namespace Humanoid.Editor.Data
{
    public class HumanoidAppearanceBodyUIData
    {
        public int Id;
        public HumanoidAppearanceRace Races;
        public List<HumanoidModelInfoData> Models;
        public HumanoidAppearanceColor Color;
    }
}