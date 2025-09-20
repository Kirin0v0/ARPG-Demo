using Animancer;
using Damage;
using Damage.Data;
using Sirenix.OdinInspector;
using UnityEngine;
using VContainer;

namespace Character.Ability.Animation
{
    public class CharacterAnimationDefenceAudioListener: BaseCharacterAnimationListener
    {
        [Title("音效配置")] [SerializeField] private AudioClip audioClip;
        [SerializeField, Range(0f, 1f)] private float volume = 1f;

        [Inject] private DamageManager _damageManager;

        private CharacterObject _character;
        
        protected override void OnAnimationPlayed(CharacterObject character, AnimancerState animancerState)
        {
            _character = character;
            _damageManager.AfterDamageHandled += HandleDamage;
        }

        protected override void OnAnimationStopped(CharacterObject character, AnimancerState animancerState)
        {
            _damageManager.AfterDamageHandled -= HandleDamage;
            _character = null;
        }

        private void HandleDamage(DamageInfo damageInfo)
        {
            if ((damageInfo.TriggerFlags & DamageInfo.DefenceFlag) != 0)
            {
                if (audioClip)
                {
                    _character.AudioAbility?.PlaySound(audioClip, false, volume);
                }
            }
        }
    }
}