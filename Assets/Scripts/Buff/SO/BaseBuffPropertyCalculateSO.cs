using Character.Data;
using UnityEngine;

namespace Buff.SO
{
    public abstract class BaseBuffPropertyCalculateSO: ScriptableObject
    {
        public abstract (CharacterProperty plus, CharacterPropertyMultiplier times) CalculateBuffProperty(Runtime.Buff buff);
    }
}