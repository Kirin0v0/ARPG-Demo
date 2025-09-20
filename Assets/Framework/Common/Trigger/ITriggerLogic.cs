using UnityEngine;

namespace Framework.Common.Trigger
{
    public interface ITriggerLogic
    {
        void EnterTriggerChain(Object target);
        void StayTriggerChain(Object target);
        void ExitTriggerChain(Object target);
    }
}