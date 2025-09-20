using System.Collections.Generic;
using System.Linq;
using Framework.Common.Blackboard;
using Framework.Common.Debug;
using UnityEngine;

namespace Framework.Common.BehaviourTree.Node.Action
{
    [NodeMenuItem("Action/Blackboard Variable")]
    public class BlackboardVariableNode : ActionNode, IBlackboardProvide
    {
        [SerializeField] private List<BlackboardVariable> variables;

        public override string Description => "黑板赋值节点，给黑板变量赋值";

        protected override NodeState OnTick(float deltaTime, object payload)
        {
            foreach (var variable in variables)
            {
                blackboard.SetParameter(variable);
            }

            return NodeState.Success;
        }

        public Blackboard.Blackboard Blackboard => blackboard;
    }
}