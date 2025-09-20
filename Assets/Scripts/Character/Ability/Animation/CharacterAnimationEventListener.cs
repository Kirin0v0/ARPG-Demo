using Animancer;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Character.Ability.Animation
{
    public enum CharacterAnimationEventCallback
    {
        Play,
        End,
        Custom,
        Stop,
    }

    public abstract class CharacterAnimationEventListener : BaseCharacterAnimationListener
    {
        protected CharacterObject Character;
        protected AnimancerState AnimancerState;

        [Title("事件配置")] [SerializeField] private CharacterAnimationEventCallback eventCallback;

        [SerializeField, EventNames, ShowIf("@eventCallback == CharacterAnimationEventCallback.Custom")]
        private StringAsset eventName;

        protected override void OnAnimationPlayed(CharacterObject character, AnimancerState animancerState)
        {
            Character = character;
            AnimancerState = animancerState;
            switch (eventCallback)
            {
                case CharacterAnimationEventCallback.Play:
                {
                    HandleEventTriggered();
                }
                    break;
                case CharacterAnimationEventCallback.End:
                {
                    animancerState.SharedEvents.OnEnd += HandleEventTriggered;
                }
                    break;
                case CharacterAnimationEventCallback.Custom:
                {
                    animancerState.SharedEvents.AddCallbacks(eventName, HandleEventTriggered);
                }
                    break;
            }
        }

        protected override void OnAnimationStopped(CharacterObject character, AnimancerState animancerState)
        {
            switch (eventCallback)
            {
                case CharacterAnimationEventCallback.Stop:
                {
                    HandleEventTriggered();
                }
                    break;
                case CharacterAnimationEventCallback.End:
                {
                    animancerState.SharedEvents.OnEnd -= HandleEventTriggered;
                }
                    break;
                case CharacterAnimationEventCallback.Custom:
                {
                    animancerState.SharedEvents.RemoveCallbacks(eventName, HandleEventTriggered);
                }
                    break;
            }

            AnimancerState = null;
            Character = null;
        }

        private void HandleEventTriggered()
        {
            if (eventCallback == CharacterAnimationEventCallback.End && !AnimancerState.IsLooping)
            {
                AnimancerState.SharedEvents.OnEnd -= HandleEventTriggered;
            }

            OnEventTriggered();
        }

        protected abstract void OnEventTriggered();
    }
}