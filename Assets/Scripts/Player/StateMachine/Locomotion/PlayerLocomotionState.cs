using System;
using Animancer;
using Framework.Common.Audio;
using Framework.Common.Debug;
using Framework.Common.StateMachine;
using Player.Brain;
using Player.StateMachine.Action;
using Player.StateMachine.Base;
using Player.StateMachine.Defence;
using Player.StateMachine.Locomotion;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Player.StateMachine.Locomotion
{
    public abstract class PlayerLocomotionState : PlayerState, IPlayerStateLocomotion, IPlayerStateLocomotionParameter
    {
        [Title("状态属性")] [SerializeField] private bool forwardLocomotion;
        [SerializeField] protected StringAsset forwardSpeedParameter;
        [SerializeField] private bool lateralLocomotion;
        [SerializeField] protected StringAsset lateralSpeedParameter;

        public bool ForwardLocomotion => forwardLocomotion;
        public bool LateralLocomotion => lateralLocomotion;
        public StringAsset ForwardSpeedParameter => forwardSpeedParameter;
        public StringAsset LateralSpeedParameter => lateralSpeedParameter;
    }
}