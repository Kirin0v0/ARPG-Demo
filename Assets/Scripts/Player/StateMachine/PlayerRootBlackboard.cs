using System;
using Framework.Common.StateMachine;
using Sirenix.OdinInspector;

namespace Player.StateMachine
{
    [Serializable]
    public class PlayerRootBlackboard : StateMachineBlackboard
    {
        [ReadOnly] public PlayerCharacterObject Owner;

        public override void Clear()
        {
            Owner = null;
        }
    }
}