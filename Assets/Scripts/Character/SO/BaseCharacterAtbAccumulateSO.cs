using Character.Data;
using UnityEngine;

namespace Character.SO
{
    public abstract class BaseCharacterAtbAccumulateSO: ScriptableObject
    {
        public abstract float AccumulateAtb(CharacterProperty property, float time);
    }
}