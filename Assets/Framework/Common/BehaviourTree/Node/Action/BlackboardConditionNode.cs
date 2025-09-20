using System.Collections.Generic;
using System.Linq;
using Framework.Common.Blackboard;
using Framework.Common.Debug;
using UnityEngine;

namespace Framework.Common.BehaviourTree.Node.Action
{
    [NodeMenuItem("Action/Blackboard Condition")]
    public class BlackboardConditionNode : ActionNode, IBlackboardProvide
    {
        [SerializeField] private List<BlackboardCondition> conditions;

        public override string Description => "黑板条件节点，控制行为树执行分支";

        protected override NodeState OnTick(float deltaTime, object payload)
        {
            if (conditions.Count == 0 || conditions.All(condition => condition.Satisfy(blackboard)))
            {
                return NodeState.Success;
            }

            return NodeState.Failure;
        }

        public Blackboard.Blackboard Blackboard => blackboard;
    }
}