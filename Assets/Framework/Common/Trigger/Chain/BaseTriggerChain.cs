using UnityEngine;

namespace Framework.Common.Trigger.Chain
{
    public abstract class BaseTriggerChain : MonoBehaviour, ITriggerChain
    {
        public abstract void Begin(Collider collider);

        public abstract void Finish(Collider collider);
    }
}