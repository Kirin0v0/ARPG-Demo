namespace Framework.Common.BehaviourTree.Node.Decorator
{
    [NodeMenuItem("Decorator/Always Failure")]
    public class AlwaysFailureNode : DecoratorNode
    {
        public override string Description => "始终返回失败的节点";

        protected override NodeState OnTick(float deltaTime, object payload)
        {
            child.Tick(deltaTime, payload);
            child.Abort(payload);
            return NodeState.Failure;
        }
    }
}