using Animancer;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Character.Ability.Animation
{
    public abstract class BaseCharacterAnimationListener: MonoBehaviour, ICharacterAnimationListen
    {
        [Title("动画配置")] [SerializeField] private bool useStringAsset = false;

        [SerializeField] [ShowIf("@useStringAsset")]
        private StringAsset animationStringAsset;

        [SerializeField] [ShowIf("@!useStringAsset")]
        private TransitionAsset animationTransition;
        
        public void HandleAnimationPlayed(CharacterObject character, AnimancerLayer animancerLayer,
            AnimancerState animancerState)
        {
            if (useStringAsset)
            {
                if (animancerLayer.Graph.States.TryGet(animationStringAsset.Key, out var state) &&
                    state == animancerState)
                {
                    OnAnimationPlayed(character, animancerState);
                }
            }
            else
            {
                var state = animancerLayer.GetOrCreateState(animationTransition);
                if (state == animancerState)
                {
                    OnAnimationPlayed(character, animancerState);
                }
            }
        }

        public void HandleAnimationStopped(CharacterObject character, AnimancerLayer animancerLayer,
            AnimancerState animancerState)
        {
            if (useStringAsset)
            {
                if (animancerLayer.Graph.States.TryGet(animationStringAsset.Key, out var state) &&
                    state == animancerState)
                {
                    OnAnimationStopped(character, animancerState);
                }
            }
            else
            {
                var state = animancerLayer.GetOrCreateState(animationTransition);
                if (state == animancerState)
                {
                    OnAnimationStopped(character, animancerState);
                }
            }
        }
        
        protected abstract void OnAnimationPlayed(CharacterObject character, AnimancerState animancerState);
        
        protected abstract void OnAnimationStopped(CharacterObject character, AnimancerState animancerState);
    }
}