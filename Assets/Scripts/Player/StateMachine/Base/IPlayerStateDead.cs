namespace Player.StateMachine.Base
{
    public enum PlayerDeadPose
    {
        Reclining,
        Countersunk,
    }
    
    public interface IPlayerStateDead
    {
        PlayerDeadPose DeadPose { get; }
    }
}