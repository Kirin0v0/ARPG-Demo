using UnityEngine;

namespace Framework.Common.Trigger.Chain
{
    public abstract class BaseTriggerPoint<T> : FeatureTriggerLogic<T> where T : Object
    {
        public sealed override void EnterTriggerChain(T target)
        {
            Trigger(target);
        }

        public sealed override void StayTriggerChain(T target)
        {
        }

        public sealed override void ExitTriggerChain(T target)
        {
        }

        public abstract void Trigger(T target);
    }
}