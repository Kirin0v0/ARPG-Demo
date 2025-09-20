namespace Framework.Common.BehaviourTree.Node.Decorator
{
    [NodeMenuItem("Decorator/Always Success")]
    public class AlwaysSuccessNode: DecoratorNode
    {
        public override string Description => "始终返回成功的节点";

        protected override NodeState OnTick(float deltaTime, object payload)
        {
            child.Tick(deltaTime, payload);
            child.Abort(payload);
            return NodeState.Success;
        }
    }
}