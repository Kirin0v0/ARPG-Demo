using System.Collections.Generic;
using Framework.Common.StateMachine;
using UnityEngine;

namespace Player.StateMachine.Base
{
    public abstract class PlayerStateMachine<TState> : StateMachine<TState>, IPlayerState
        where TState : MonoBehaviour, IState, IPlayerState
    {
        public bool ControlRootMotionBySelf()
        {
            if (CurrentState)
            {
                return CurrentState.ControlRootMotionBySelf();
            }

            return false;
        }

        public (Vector3? deltaPosition, Quaternion? deltaRotation, bool useCharacterController)
            CalculateRootMotionDelta(Animator animator)
        {
            if (CurrentState)
            {
                return CurrentState.CalculateRootMotionDelta(animator);
            }

            return (null, null, false);
        }

        public void HandleAnimatorIK(Animator animator)
        {
            if (CurrentState)
            {
                CurrentState.HandleAnimatorIK(animator);
            }
        }

        public void ShowStateName(string stateName)
        {
            if (CurrentState)
            {
                CurrentState.ShowStateName(stateName);
            }
        }

        public void ShowTransition(PlayerStateTransition transition)
        {
            if (CurrentState)
            {
                CurrentState.ShowTransition(transition);
            }
        }

        public string GetStateName()
        {
            return CurrentState ? CurrentState.GetStateName() : "";
        }

        public List<PlayerStateTransition> GetStateTransitions()
        {
            return CurrentState ? CurrentState.GetStateTransitions() : new List<PlayerStateTransition>();
        }
    }
}