using UnityEngine;

namespace Framework.Common.BehaviourTree.Node.Composite
{
    [NodeMenuItem("Composite/Await All Success")]
    public class AwaitAllSuccessNode : CompositeNode
    {
        [SerializeField] private bool stateless = false;

        private int _lastTickIndex;

        public override string Description => "等待全部子节点成功的节点，依次执行子节点，直到子节点全部成功才会返回成功";

        protected override void OnStart(object payload)
        {
            _lastTickIndex = -1;
        }

        protected override NodeState OnTick(float deltaTime, object payload)
        {
            var index = stateless ? 0 : (_lastTickIndex < 0 ? 0 : _lastTickIndex);
            if (index >= children.Count)
            {
                return NodeState.Success;
            }

            // 一帧内线性执行子节点
            while (index < children.Count)
            {
                var child = children[index];
                var nodeState = child.Tick(deltaTime, payload);
                // 如果子节点处于运行或失败状态就不接着执行后续子节点
                if (nodeState == NodeState.Running || nodeState == NodeState.Failure)
                {
                    // 如果处于无状态，就打断上一帧的运行状态节点
                    if (stateless && index != _lastTickIndex && _lastTickIndex >= 0)
                    {
                        children[_lastTickIndex].Abort(payload);
                    }

                    _lastTickIndex = index;
                    return NodeState.Running;
                }

                if (nodeState == NodeState.Success)
                {
                    index++;
                }
            }

            return NodeState.Success;
        }
    }
}