using System;
using System.Collections.Generic;
using UnityEngine;

namespace Character.Data
{
    [Serializable]
    public struct CharacterDrop
    {
        public int experience;
        public int money;
        public List<int> packages;

        public static CharacterDrop Empty = new CharacterDrop
        {
            experience = 0,
            money = 0,
            packages = new List<int>(),
        };
    }
}