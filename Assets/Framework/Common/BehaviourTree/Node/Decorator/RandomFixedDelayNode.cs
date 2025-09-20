using UnityEngine;

namespace Framework.Common.BehaviourTree.Node.Decorator
{
    [NodeMenuItem("Decorator/Random Fixed Delay")]
    public class RandomFixedDelayNode : DecoratorNode
    {
        [SerializeField] private float minDelayTime = 0f;
        [SerializeField] private float maxDelayTime = 1f;
        [SerializeField] private bool recordTimeAfterAbort = false;

        private float _startTime;
        private float _duration;

        public override string Description => "固定延迟节点，不受时间缩放影响";

        protected override void OnStart(object payload)
        {
            _startTime = Time.unscaledTime;
            _duration = Random.Range(minDelayTime, maxDelayTime);
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
            if (Time.unscaledTime - _startTime >= _duration)
            {
                return child.Tick(deltaTime, payload);
            }

            return NodeState.Running;
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