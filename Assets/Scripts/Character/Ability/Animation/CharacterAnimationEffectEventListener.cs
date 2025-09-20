using Animancer;
using Framework.Common.Debug;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace Character.Ability.Animation
{
    public class CharacterAnimationEffectEventListener : BaseCharacterAnimationListener
    {
        [Title("特效配置")] [SerializeField] private GameObject prefab;
        [SerializeField] private CharacterEffectPosition position =CharacterEffectPosition.Center;
        [SerializeField] private Vector3 localPosition;
        [SerializeField] private Quaternion localRotation;
        [SerializeField] private Vector3 localScale = Vector3.one;

        private string _effectId;

        protected override void OnAnimationPlayed(CharacterObject character, AnimancerState animancerState)
        {
            _effectId = character.EffectAbility?.AddEffect(prefab, localPosition, localRotation, localScale, position);
        }

        protected override void OnAnimationStopped(CharacterObject character, AnimancerState animancerState)
        {
            character.EffectAbility?.RemoveEffect(_effectId);
        }
    }
}