using Character.Data;
using UnityEngine;

namespace Character.SO
{
    public abstract class BaseCharacterDefenceAlgorithmSO: ScriptableObject
    {
        public abstract int CalculateDefence(CharacterProperty property);
    }
}