using System;
using System.Collections.Generic;
using Character.Data;
using Framework.DataStructure;
using UnityEngine;

namespace Humanoid.Data
{
    [Serializable]
    public class HumanoidArchiveData : CharacterArchiveData
    {
        public HumanoidCharacterRace race = HumanoidCharacterRace.HumanMale;
        public HumanoidAppearanceArchiveData appearance = new();
    }

    [Serializable]
    public class HumanoidAppearanceArchiveData
    {
        public HumanoidBodyAppearanceArchiveData body = new();
        public HumanoidGearAppearanceArchiveData headGear = new();
        public HumanoidGearAppearanceArchiveData torsoGear = new();
        public HumanoidGearAppearanceArchiveData leftArmGear = new();
        public HumanoidGearAppearanceArchiveData rightArmGear = new();
        public HumanoidGearAppearanceArchiveData leftLegGear = new();
        public HumanoidGearAppearanceArchiveData rightLegGear = new();
    }

    [Serializable]
    public class HumanoidBodyAppearanceArchiveData
    {
        public List<int> models = new();
        public HumanoidAppearanceColorArchiveData color = new();
    }

    [Serializable]
    public class HumanoidGearAppearanceArchiveData
    {
        public List<int> models = new();
        public HumanoidAppearanceColorArchiveData color = new();
    }

    [Serializable]
    public class HumanoidAppearanceColorArchiveData
    {
        public SerializableColor skinColor = SerializableColor.Clear();
        public SerializableColor hairColor = SerializableColor.Clear();
        public SerializableColor stubbleColor = SerializableColor.Clear();
        public SerializableColor eyesColor = SerializableColor.Clear();
        public SerializableColor scarColor = SerializableColor.Clear();
        public SerializableColor bodyArtColor = SerializableColor.Clear();
        public SerializableColor primaryColor = SerializableColor.Clear();
        public SerializableColor secondaryColor = SerializableColor.Clear();
        public SerializableColor metalPrimaryColor = SerializableColor.Clear();
        public SerializableColor metalSecondaryColor = SerializableColor.Clear();
        public SerializableColor metalDarkColor = SerializableColor.Clear();
        public SerializableColor leatherPrimaryColor = SerializableColor.Clear();
        public SerializableColor leatherSecondaryColor = SerializableColor.Clear();
    }
}