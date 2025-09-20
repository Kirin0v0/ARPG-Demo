using Action;

namespace Combo
{
    public interface IComboPlay
    {
        ComboStage Stage { get; }
        event System.Action<ComboStage> OnStageChanged;

        void Start();
        void Tick(float deltaTime);
        void Stop();
    }
}