namespace Framework.Common.BehaviourTree.Node.Decorator
{
    [NodeMenuItem("Decorator/Loop")]
    public class LoopNode: DecoratorNode
    {
        protected override NodeState OnTick(float deltaTime, object payload)
        {
            child.Tick(deltaTime, payload);
            return NodeState.Running;
        }
    }
}