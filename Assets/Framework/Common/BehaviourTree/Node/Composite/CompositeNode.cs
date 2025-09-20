using System.Collections.Generic;
using System.Linq;
using Framework.Common.Debug;
using Framework.Core.Attribute;

namespace Framework.Common.BehaviourTree.Node.Composite
{
    public abstract class CompositeNode : Node
    {
        [DisplayOnly] public List<Node> children = new();

        public override string Description => "复合节点";

        public override Node Clone()
        {
            var node = Instantiate(this);
            node.children = children.ConvertAll(node => node.Clone());
            return node;
        }

        protected override void OnAbort(object payload)
        {
            children.ForEach(child => { child.Abort(payload); });
        }

#if UNITY_EDITOR
        public override bool AddChildNode(Node child)
        {
            children.Add(child);
            child.executeOrder = children.Count;
            return true;
        }

        public override bool RemoveChildNode(Node child)
        {
            var remove = children.Remove(child);
            child.executeOrder = 1;
            // 剩余子节点执行顺序重新设置
            for (var i = 0; i < children.Count; i++)
            {
                var childNode = children[i];
                childNode.executeOrder = i + 1;
            }

            return remove;
        }
#endif
    }
}