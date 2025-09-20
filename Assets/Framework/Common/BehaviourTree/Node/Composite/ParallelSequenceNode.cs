using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Framework.Common.BehaviourTree.Node.Composite
{
    [NodeMenuItem("Composite/Parallel Sequence")]
    public class ParallelSequenceNode : CompositeNode
    {
        private readonly HashSet<Node> _completeNodes = new();
        private bool _findFailure;
        
        public override string Description => "并行序列节点，同时执行子节点，进行与逻辑判断，等待某个节点返回失败并打断其他节点执行";

        protected override void OnStart(object payload)
        {
            _completeNodes.Clear();
            _findFailure = false;
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
                    case NodeState.Success:
                        _completeNodes.Add(child);
                        break;
                    case NodeState.Failure:
                        _completeNodes.Add(child);
                        _findFailure = true;
                        break;
                }
            }

            // 如果找到失败节点就打断其他正在运行的节点
            if (_findFailure)
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