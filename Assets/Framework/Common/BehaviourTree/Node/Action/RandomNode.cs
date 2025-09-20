using UnityEngine;

namespace Framework.Common.BehaviourTree.Node.Action
{
    [NodeMenuItem("Action/Random")]
    public class RandomNode: ActionNode
    {
        [SerializeField, Range(0f, 1f)] private float probability;

        public override string Description => "随机概率节点，每次执行时随机生成数判断是否满足概率范围内";

        protected override NodeState OnTick(float deltaTime, object payload)
        {
            return Random.value <= probability ? NodeState.Success : NodeState.Failure;
        }
    }
}