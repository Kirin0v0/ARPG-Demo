using Animancer;

namespace Character.Ability.Animation
{
    public interface ICharacterAnimationListen
    {
        void HandleAnimationPlayed(CharacterObject character, AnimancerLayer animancerLayer,
            AnimancerState animancerState);

        void HandleAnimationStopped(CharacterObject character, AnimancerLayer animancerLayer,
            AnimancerState animancerState);
    }
}