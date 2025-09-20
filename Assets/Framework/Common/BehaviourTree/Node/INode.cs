namespace Framework.Common.BehaviourTree.Node
{
    public enum NodeState
    {
        Running,
        Failure,
        Success,
    }

    public interface INode
    {
        NodeState State { get; }
        void Reset(object payload);
        NodeState Tick(float deltaTime, object payload);
        void Abort(object payload);
    }
}