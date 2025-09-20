namespace Framework.Common.BehaviourTree.Node.Decorator
{
    [NodeMenuItem("Decorator/Reverse")]
    public class ReverseNode : DecoratorNode
    {
        public override string Description => "返回相反结果的节点";

        protected override NodeState OnTick(float deltaTime, object payload)
        {
            if (child.Tick(deltaTime, payload) == NodeState.Running)
            {
                return NodeState.Running;
            }

            return child.Tick(deltaTime, payload) == NodeState.Success ? NodeState.Failure : NodeState.Success;
        }
    }
}