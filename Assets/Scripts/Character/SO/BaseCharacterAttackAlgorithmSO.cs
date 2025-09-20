using Character.Data;
using UnityEngine;

namespace Character.SO
{
    public abstract class BaseCharacterAttackAlgorithmSO: ScriptableObject
    {
        public abstract (int physicsAttack, int magicAttack) CalculateAttack(CharacterProperty property);
    }
}