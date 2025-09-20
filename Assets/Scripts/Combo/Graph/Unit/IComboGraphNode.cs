using JetBrains.Annotations;

namespace Combo.Graph.Unit
{
    public interface IComboGraphNode
    {
        void Enter([CanBeNull] IComboGraphNode from, [CanBeNull] ComboGraphTransition transition,
            ComboGraphParameters parameters);

        void Tick(float deltaTime, ComboGraphParameters parameters);

        void Exit([CanBeNull] IComboGraphNode to, [CanBeNull] ComboGraphTransition transition,
            ComboGraphParameters parameters);

        void Abort(ComboGraphParameters parameters);
    }
}