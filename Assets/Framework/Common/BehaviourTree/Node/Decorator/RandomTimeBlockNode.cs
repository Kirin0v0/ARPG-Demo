using Framework.Common.Debug;
using UnityEngine;

namespace Framework.Common.BehaviourTree.Node.Decorator
{
    [NodeMenuItem("Decorator/Random Time Block")]
    public class RandomTimeBlockNode : DecoratorNode
    {
        [SerializeField] private float minDuration = 0f;
        [SerializeField] private float maxDuration = 1f;
        [SerializeField] private bool recordTimeAfterAbort = false;
        
        private float _abortTime;
        private float _time;
        private float _duration;

        public override string Description => "时间阻塞节点，受时间缩放影响，在一段时间内执行子节点但返回运行中，到时后返回成功";

        protected override void OnStart(object payload)
        {
            _time = 0f;
            _duration = Random.Range(minDuration, maxDuration);
        }

        protected override void OnResume(object payload)
        {
            if (recordTimeAfterAbort)
            {
                _time += Tree.Time - _abortTime;
            }
        }

        protected override NodeState OnTick(float deltaTime, object payload)
        {
            _time += deltaTime;
            if (_time < _duration)
            {
                child.Tick(deltaTime, payload);
                return NodeState.Running;
            }

            child.Abort(payload);
            return NodeState.Success;
        }

        protected override void OnAbort(object payload)
        {
            base.OnAbort(payload);
            if (recordTimeAfterAbort)
            {
                _abortTime = Tree.Time;
            }
        }
    }
}