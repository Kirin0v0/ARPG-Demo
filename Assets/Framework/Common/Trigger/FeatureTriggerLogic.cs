using UnityEngine;

namespace Framework.Common.Trigger
{
    public abstract class FeatureTriggerLogic<T> : BaseTriggerLogic where T : Object
    {
        public sealed override void EnterTriggerChain(Object target)
        {
            EnterTriggerChain((T)target);
        }

        public sealed override void StayTriggerChain(Object target)
        {
            StayTriggerChain((T)target);
        }

        public sealed override void ExitTriggerChain(Object target)
        {
            ExitTriggerChain((T)target);
        }

        public abstract void EnterTriggerChain(T target);
        public abstract void StayTriggerChain(T target);
        public abstract void ExitTriggerChain(T target);
    }
}