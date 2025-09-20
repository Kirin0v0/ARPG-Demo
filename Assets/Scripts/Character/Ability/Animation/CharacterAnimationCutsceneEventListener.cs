using Animancer;
using Common;
using Events;
using Events.Data;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;
using VContainer;

namespace Character.Ability.Animation
{
    public class CharacterAnimationCutsceneEventListener : CharacterAnimationEventListener
    {
        [Title("延迟配置")] [SerializeField] private float showDelay = 0f;

        [Title("过场配置")] [SerializeField] private string title;
        [SerializeField] private AudioClip audioClip;
        [SerializeField, Min(0f)] private float duration;
        [SerializeField] private UnityEvent<CharacterObject> onFinished;

        protected override void OnEventTriggered()
        {
            if (showDelay > 0)
            {
                Invoke(nameof(SendCutsceneEvent), showDelay);
            }
            else
            {
                SendCutsceneEvent();
            }
        }

        private void SendCutsceneEvent()
        {
            GameApplication.Instance.EventCenter.TriggerEvent(GameEvents.Cutscene, new CutsceneEventParameter
            {
                Title = title,
                Audio = audioClip,
                Duration = duration,
                OnFinished = () => { onFinished?.Invoke(Character); }
            });
        }
    }
}