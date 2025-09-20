using System;

namespace Framework.Common.StateMachine
{
    [Serializable]
    public abstract class StateMachineBlackboard
    {
        public virtual void Clear()
        {
        }
    }
}