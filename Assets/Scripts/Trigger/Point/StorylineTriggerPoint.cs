using Character;
using Common;
using Events;
using Events.Data;
using Framework.Common.Debug;
using Framework.Common.Trigger;
using Framework.Common.Trigger.Chain;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace Trigger.Point
{
    public class StorylineTriggerPoint:  BaseTriggerPoint<CharacterObject>
    {
        [SerializeField] private string title;
        [SerializeField] private AudioClip audioClip;
        [SerializeField, Min(0f)] private float duration;
        [SerializeField] private UnityEvent<CharacterObject> onFinished;

        public override void Trigger(CharacterObject target)
        {
            GameApplication.Instance.EventCenter.TriggerEvent(GameEvents.Cutscene, new CutsceneEventParameter
            {
                Title = title,
                Audio = audioClip,
                Duration = duration,
                OnFinished = () => { onFinished?.Invoke(target); }
            });
        }

        public override BaseTriggerLogic Clone(GameObject gameObject)
        {
            gameObject.name = "Storyline Trigger Point";
            var triggerPoint = gameObject.AddComponent<StorylineTriggerPoint>();
            triggerPoint.title = title;
            triggerPoint.audioClip = audioClip;
            triggerPoint.duration = duration;
            triggerPoint.onFinished = onFinished;
            return triggerPoint;
        }
    }
}