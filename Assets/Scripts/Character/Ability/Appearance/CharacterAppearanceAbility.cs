using UnityEngine;

namespace Character.Ability.Appearance
{
    public abstract class CharacterAppearanceAbility : BaseCharacterOptionalAbility
    {
        public abstract void SetAppearance(object[] payload);
    }
}