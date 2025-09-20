using Animancer;
using Common;
using Framework.Common.Audio;
using Sirenix.OdinInspector;
using UnityEngine;
using VContainer;

namespace Character.Ability.Animation
{
    public class CharacterAnimationRandomAudioEventListener : CharacterAnimationEventListener
    {
        [Title("音效配置")] [SerializeField] private AudioClipRandomizer audioClipRandomizer;
        [SerializeField, Range(0f, 1f)] private float volume = 1f;

        protected override void OnEventTriggered()
        {
            if (audioClipRandomizer)
            {
                Character.AudioAbility?.PlaySound(audioClipRandomizer.Random(), false, volume);
            }
        }
    }
}