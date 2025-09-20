using UnityEngine;

namespace Framework.Common.Trigger.Chain
{
    public interface ITriggerChain
    {
        
        void Begin(Collider collider);
        void Finish(Collider collider);
    }
}