using System.Collections.Generic;
using Humanoid.Data;
using Humanoid.Model.Data;
using UnityEngine;

namespace Humanoid.Editor.Data
{
    public class HumanoidAppearanceWeaponUIData
    {
        public int Id = 0;
        public string Model = "";
        public HumanoidAppearanceWeaponType Type = HumanoidAppearanceWeaponType.Fantasy;
        public string Comment = "";
        public HumanoidAppearanceColor FantasyColor = default;
        public string SamuraiTexture = "";
    }
}