using UnityEngine;

namespace Framework.Common.Trigger.Chain
{
    public abstract class BaseTriggerProcess<T> : FeatureTriggerLogic<T> where T : Object
    {
        public abstract override void EnterTriggerChain(T target);
        public abstract override void StayTriggerChain(T target);
        public abstract override void ExitTriggerChain(T target);
    }
}