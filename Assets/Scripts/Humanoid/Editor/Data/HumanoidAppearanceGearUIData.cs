using System.Collections.Generic;
using Humanoid.Data;
using Humanoid.Model.Data;

namespace Humanoid.Editor.Data
{
    public class HumanoidAppearanceGearUIData
    {
        public int Id;
        public HumanoidAppearanceRace Races;
        public List<HumanoidModelInfoData> Models;
        public HumanoidAppearanceGearPart Part;
        public string Mark;
        public HumanoidAppearanceColor Color;
    }
}