using Animancer;
using Common;
using Framework.Common.Audio;
using Framework.Common.Debug;
using Sirenix.OdinInspector;
using UnityEngine;
using VContainer;

namespace Character.Ability.Animation
{
    public class CharacterAnimationAudioEventListener : CharacterAnimationEventListener
    {
        [Title("音效配置")] [SerializeField] private AudioClip audioClip;
        [SerializeField, Range(0f, 1f)] private float volume = 1f;

        private AnimancerState _animancerState;

        protected override void OnEventTriggered()
        {
            if (audioClip)
            {
                Character.AudioAbility?.PlaySound(audioClip, false, volume);
            }
        }
    }
}