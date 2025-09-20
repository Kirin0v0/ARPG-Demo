using System.Collections.Generic;

namespace Framework.Common.BehaviourTree.Node.Composite
{
    [NodeMenuItem("Composite/Parallel Selector")]
    public class ParallelSelectorNode : CompositeNode
    {
        private readonly HashSet<Node> _completeNodes = new();
        private bool _findSuccess = false;

        public override string Description => "并行选择节点，同时执行子节点，进行或逻辑判断，等待某个节点返回成功并打断其他节点执行";

        protected override void OnStart(object payload)
        {
            _completeNodes.Clear();
            _findSuccess = false;
        }

        protected override NodeState OnTick(float deltaTime, object payload)
        {
            foreach (var child in children)
            {
                if (_completeNodes.Contains(child))
                {
                    continue;
                }

                switch (child.Tick(deltaTime, payload))
                {
                    case NodeState.Failure:
                        _completeNodes.Add(child);
                        break;
                    case NodeState.Success:
                        _completeNodes.Add(child);
                        _findSuccess = true;
                        break;
                }
            }

            // 如果找到成功节点就打断其他正在运行的节点
            if (_findSuccess)
            {
                foreach (var child in children)
                {
                    if (_completeNodes.Contains(child))
                    {
                        continue;
                    }

                    child.Abort(payload);
                }

                return NodeState.Failure;
            }

            // 全部成功则返回成功
            if (_completeNodes.Count >= children.Count)
            {
                return NodeState.Success;
            }

            return NodeState.Running;
        }
    }
}