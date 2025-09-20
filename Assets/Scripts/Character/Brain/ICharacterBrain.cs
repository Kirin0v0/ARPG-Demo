namespace Character.Brain
{
    public interface ICharacterBrain
    {
        void Init(CharacterObject owner);
        void UpdateRenderThoughts(float deltaTime);
        void UpdateLogicThoughts(float fixedDeltaTime);
        void Destroy();
    }
}