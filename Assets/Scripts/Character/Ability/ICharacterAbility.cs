namespace Character.Ability
{
    public interface ICharacterAbility
    {
        public void Init(CharacterObject owner);
        public void Dispose();
    }
}