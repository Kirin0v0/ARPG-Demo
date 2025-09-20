using Character;
using Character.Ability;
using UnityEngine;

namespace Player.Ability
{
    public class PlayerAtbRewardAbility: CharacterAtbRewardAbility
    {
        private new PlayerCharacterObject Owner => base.Owner as PlayerCharacterObject;
        
        [SerializeField] private float stunnedReward = 0.05f;
        [SerializeField] private float brokenReward = 0.2f;
        [SerializeField] private float deadReward = 0.3f;
        [SerializeField] private float perfectEvadeReward = 0.05f;
        [SerializeField] private float perfectDefendReward = 0.05f;
        
        public override float RewardMakeTargetStunned(CharacterObject target)
        {
            return stunnedReward;
        }

        public override float RewardMakeTargetBroken(CharacterObject target)
        {
            return brokenReward;
        }

        public override float RewardMakeTargetDead(CharacterObject target)
        {
            return deadReward;
        }

        public float RewardAvoidDamageByPerfectEvade(CharacterObject attacker)
        {
            return perfectEvadeReward;
        }

        public float RewardAvoidDamageByPerfectDefend(CharacterObject attacker)
        {
            return perfectDefendReward;
        }
    }
}