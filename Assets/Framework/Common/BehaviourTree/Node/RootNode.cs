using Framework.Core.Attribute;

namespace Framework.Common.BehaviourTree.Node
{
    public class RootNode : Node
    {
        [DisplayOnly] public Node child;

        public override string Description => "根节点";

        protected override void OnStart(object payload)
        {
        }

        protected override NodeState OnTick(float deltaTime, object payload)
        {
            return child?.Tick(deltaTime, payload) ?? NodeState.Success;
        }

        protected override void OnAbort(object payload)
        {
            child?.Abort(payload);
        }

        protected override void OnStop(object payload)
        {
        }

        public override Node Clone()
        {
            var rootNode = Instantiate(this);
            rootNode.child = rootNode.child?.Clone();
            return rootNode;
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