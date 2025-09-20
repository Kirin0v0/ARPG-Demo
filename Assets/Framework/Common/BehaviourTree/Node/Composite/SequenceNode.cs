using UnityEngine;

namespace Framework.Common.BehaviourTree.Node.Composite
{
    [NodeMenuItem("Composite/Sequence")]
    public class SequenceNode : CompositeNode
    {
        [SerializeField] private bool stateless = false;
        
        private int _lastTickIndex;

        public override string Description => "线性序列节点，依次执行子节点，进行与逻辑判断";

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
                // 如果子节点处于运行状态就不接着执行后续子节点
                if (nodeState == NodeState.Running)
                {
                    // 如果处于无状态，就打断上一帧的运行状态节点
                    if (stateless && index != _lastTickIndex && _lastTickIndex >= 0)
                    {
                        children[_lastTickIndex].Abort(payload);
                    }

                    _lastTickIndex = index;
                    return NodeState.Running;
                }

                switch (nodeState)
                {
                    case NodeState.Failure:
                        // 如果处于无状态，就打断上一帧的运行状态节点
                        if (stateless && index != _lastTickIndex && _lastTickIndex >= 0)
                        {
                            children[_lastTickIndex].Abort(payload);
                        }
                        return NodeState.Failure;
                    case NodeState.Success:
                        index++;
                        break;
                }
            }

            return NodeState.Success;
        }
    }
}