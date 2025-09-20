using Character;
using Common;
using Events;
using Events.Data;
using Features.Game;
using Framework.Common.Trigger;
using Framework.Common.Trigger.Chain;
using UnityEngine;

namespace Trigger.Point
{
    public class TeleportTriggerPoint :  BaseTriggerPoint<CharacterObject>
    {
        [SerializeField] private int mapId;
        [SerializeField] private Vector3 position;
        [SerializeField] private float forwardAngle;

        public override void Trigger(CharacterObject target)
        {
            GameApplication.Instance.EventCenter.TriggerEvent<TeleportEventParameter>(GameEvents.Teleport, new()
            {
                MapId = mapId,
                Position = position,
                ForwardAngle = forwardAngle,
            });
        }

        public override BaseTriggerLogic Clone(GameObject gameObject)
        {
            gameObject.name = "Teleport Trigger Point";
            var triggerPoint = gameObject.AddComponent<TeleportTriggerPoint>();
            triggerPoint.mapId = mapId;
            triggerPoint.position = position;
            triggerPoint.forwardAngle = forwardAngle;
            return triggerPoint;
        }
    }
}