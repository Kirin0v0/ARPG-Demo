using UnityEngine;

namespace Framework.Common.BehaviourTree.Node.Decorator
{
    [NodeMenuItem("Decorator/Random Fixed Block")]
    public class RandomFixedBlockNode : DecoratorNode
    {
        [SerializeField] private float minDuration = 0f;
        [SerializeField] private float maxDuration = 1f;
        [SerializeField] private bool recordTimeAfterAbort = false;

        private float _startTime;
        private float _duration;

        public override string Description => "固定阻塞节点，不受时间缩放影响，在一段时间内执行子节点但返回运行中，到时后返回成功";

        protected override void OnStart(object payload)
        {
            _startTime = Time.unscaledTime;
            _duration = Random.Range(minDuration, maxDuration);
        }

        protected override void OnResume(object payload)
        {
            if (recordTimeAfterAbort)
            {
                _duration -= Time.unscaledTime - _startTime;
            }

            _startTime = Time.unscaledTime;
        }

        protected override NodeState OnTick(float deltaTime, object payload)
        {
            if (Time.unscaledTime - _startTime < _duration)
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
            _duration -= Time.unscaledTime - _startTime;
            if (recordTimeAfterAbort)
            {
                _startTime = Time.unscaledTime;
            }
        }
    }
}