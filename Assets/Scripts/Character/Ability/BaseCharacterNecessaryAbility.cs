namespace Character.Ability
{
    public abstract class BaseCharacterNecessaryAbility : ICharacterAbility
    {
        protected CharacterObject Owner;

        public void Init(CharacterObject owner)
        {
            Owner = owner;
            OnInit();
        }

        public void Dispose()
        {
            OnDispose();
            Owner = null;
        }

        protected virtual void OnInit()
        {
        }

        protected virtual void OnDispose()
        {
        }
    }
}