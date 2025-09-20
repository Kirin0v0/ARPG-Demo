using System;
using Character;

namespace Skill.Condition
{
    [Serializable]
    public abstract class BaseSkillPostcondition
    {
        public abstract bool MatchSkillPreconditions(
            SkillFlow skillFlow,
            CharacterObject caster,
            CharacterObject target,
            out BaseSkillPostconditionFailureReason reason
        );
    }
}