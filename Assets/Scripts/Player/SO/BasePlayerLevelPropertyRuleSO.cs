using Character.Data;
using UnityEngine;

namespace Player.SO
{
    public abstract class BasePlayerLevelPropertyRuleSO: ScriptableObject
    {
        public abstract int CalculateLevelUpExperience(int level);
        public abstract CharacterProperty CalculateLevelProperty(int level);
    }
}