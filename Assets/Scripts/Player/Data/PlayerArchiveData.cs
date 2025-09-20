using System;
using System.Collections.Generic;
using Character.Data;
using Humanoid.Data;

namespace Player.Data
{
    [Serializable]
    public class PlayerArchiveData: HumanoidArchiveData
    {
        public int level = 1;
        public int money = 0;
        public int experience = 0;
        public Dictionary<string, int> killPrototypeRecords = new();
        public Dictionary<string, int> killMapCharacterRecords = new();
    }
}