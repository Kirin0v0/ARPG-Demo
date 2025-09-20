using Framework.Common.StateMachine;
using Player.StateMachine.Base;
using UnityEngine;

namespace Player.StateMachine
{
    public class PlayerRootStateMachine : PlayerStateMachine<PlayerState>
    {
        public PlayerRootBlackboard blackboard = new();
        public override StateMachineBlackboard Blackboard => blackboard;
    }
}