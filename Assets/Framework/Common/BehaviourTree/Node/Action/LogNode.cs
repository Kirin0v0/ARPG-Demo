using UnityEngine;

namespace Framework.Common.BehaviourTree.Node.Action
{
    [NodeMenuItem("Action/Log")]
    public class LogNode : ActionNode
    {
        [TextArea, SerializeField] private string message = "Hi, I'm LogNode";

        public override string Description => "日志节点";

        protected override NodeState OnTick(float deltaTime, object payload)
        {
            UnityEngine.Debug.Log(message);
            return NodeState.Success;
        }
    }
}