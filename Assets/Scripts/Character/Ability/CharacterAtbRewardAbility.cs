using Damage.Data;
using UnityEngine;

namespace Character.Ability
{
    public abstract class CharacterAtbRewardAbility : BaseCharacterOptionalAbility
    {
        public abstract float RewardMakeTargetStunned(CharacterObject target);
        public abstract float RewardMakeTargetBroken(CharacterObject target);
        public abstract float RewardMakeTargetDead(CharacterObject target);
    }
}