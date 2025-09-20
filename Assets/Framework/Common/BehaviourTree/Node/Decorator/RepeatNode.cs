using System;
using UnityEngine;

namespace Framework.Common.BehaviourTree.Node.Decorator
{
    [NodeMenuItem("Decorator/Repeat")]
    public class RepeatNode : DecoratorNode
    {
        [SerializeField] private int repeatTimes = 1;
        private int _times;

        public override string Description => "重复节点，重复执行指定次数的子节点Tick函数";

        protected override void OnStart(object payload)
        {
            _times = 0;
        }

        protected override NodeState OnTick(float deltaTime, object payload)
        {
            if (repeatTimes < 1)
            {
                return NodeState.Success;
            }

            child.Tick(deltaTime, payload);
            _times++;
            if (_times >= repeatTimes)
            {
                child.Abort(payload);
                return NodeState.Success;
            }

            return NodeState.Running;
        }

        private void OnValidate()
        {
            if (repeatTimes < 1)
            {
                repeatTimes = 1;
            }
        }
    }
}