namespace Player.StateMachine.Base
{
    public interface IPlayerStateWeapon
    {
        bool OnlyWeapon { get; }
        bool OnlyNoWeapon { get; }
    }
}