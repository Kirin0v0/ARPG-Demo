using UnityEngine;

namespace Framework.Common.Trigger
{
    public abstract class BaseTriggerLogic : MonoBehaviour, ITriggerLogic
    {
        public abstract void EnterTriggerChain(Object target);
        public abstract void StayTriggerChain(Object target);
        public abstract void ExitTriggerChain(Object target);

        public abstract BaseTriggerLogic Clone(GameObject gameObject);
    }
}