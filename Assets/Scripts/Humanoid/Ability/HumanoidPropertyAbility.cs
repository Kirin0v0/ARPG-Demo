using Character;
using Character.Ability;
using Character.Data;
using Common;

namespace Humanoid.Ability
{
    public class HumanoidPropertyAbility : CharacterPropertyAbility
    {
        private new HumanoidCharacterObject Owner => base.Owner as HumanoidCharacterObject;
        
        public HumanoidPropertyAbility(AlgorithmManager algorithmManager) : base(algorithmManager)
        {
        }
        
        protected override CharacterProperty CalculateFinalProperty()
        {
            return (Owner.Parameters.baseProperty +
                    Owner.HumanoidParameters.weaponProperty +
                    Owner.HumanoidParameters.gearProperty +
                    Owner.Parameters.buffPlusProperty) *
                   Owner.Parameters.buffTimesProperty;
        }
    }
}