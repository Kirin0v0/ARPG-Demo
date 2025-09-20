using System.Collections.Generic;
using UnityEngine;

namespace Player.StateMachine.Base
{
    public interface IPlayerState
    {
        bool ControlRootMotionBySelf();

        (Vector3? deltaPosition, Quaternion? deltaRotation, bool useCharacterController) CalculateRootMotionDelta(
            Animator animator);

        void HandleAnimatorIK(Animator animator);

        void ShowStateName(string stateName);
        void ShowTransition(PlayerStateTransition transition);

        string GetStateName();
        List<PlayerStateTransition> GetStateTransitions();
    }
}