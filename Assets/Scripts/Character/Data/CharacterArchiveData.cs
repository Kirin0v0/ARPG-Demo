using System;
using System.Collections.Generic;

namespace Character.Data
{
    [Serializable]
    public class CharacterArchiveData
    {
        public string name = "";
        public CharacterMapArchiveData map = new();
        public int hp = int.MaxValue;
        public int mp = int.MaxValue;
        public List<string> skills = new();
    }
}