using Framework.Core.Attribute;

namespace Framework.Common.BehaviourTree.Node.Decorator
{
    public abstract class DecoratorNode : Node
    {
        [DisplayOnly] public Node child;

        public override string Description => "装饰节点";

        public override Node Clone()
        {
            var node = Instantiate(this);
            node.child = node.child?.Clone();
            return node;
        }

        protected override void OnAbort(object payload)
        {
            child?.Abort(payload);
        }

#if UNITY_EDITOR
        public override bool AddChildNode(Node child)
        {
            this.child = child;
            child.executeOrder = 1;
            return true;
        }

        public override bool RemoveChildNode(Node child)
        {
            if (this.child == child)
            {
                this.child = null;
                child.executeOrder = 1;
                return true;
            }

            return false;
        }
#endif
    }
}