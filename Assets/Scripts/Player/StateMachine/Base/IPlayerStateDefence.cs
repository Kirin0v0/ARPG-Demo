namespace Player.StateMachine.Base
{
    public enum PlayerDefenceStateType
    {
        Action,
        Locomotion,
    }
    
    public interface IPlayerStateDefence
    {
        PlayerDefenceStateType StateType { get; }
        float AllowChangeStateTime { get; }
        bool DamageResistant { get; }
    }
}