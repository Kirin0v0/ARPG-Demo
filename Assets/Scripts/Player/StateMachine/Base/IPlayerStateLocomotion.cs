using System;
using Animancer;

namespace Player.StateMachine.Base
{
    public interface IPlayerStateLocomotion
    {
        bool ForwardLocomotion { get; }
        bool LateralLocomotion { get; }
    }
    
    public interface IPlayerStateLocomotionParameter
    {
        StringAsset ForwardSpeedParameter { get; }
        StringAsset LateralSpeedParameter { get; }    
    }
}