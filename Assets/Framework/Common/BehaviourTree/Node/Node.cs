using System;
using Framework.Common.Debug;
using Framework.Core.Attribute;
using UnityEngine;
using UnityEngine.Serialization;

namespace Framework.Common.BehaviourTree.Node
{
    public abstract class Node : ScriptableObject, INode
    {
// #if UNITY_EDITOR
        [HideInInspector] public string guid;
        [HideInInspector] public Vector2 position;
        [HideInInspector] public int executeOrder = 1; // 执行顺序，从1开始
// #endif

        [TextArea] public string comment;
        [HideInInspector] public Blackboard.Blackboard blackboard;
        [SerializeField] public bool stopWhenAbort = true;
         [SerializeField] public bool debugLifecycle;
        [FormerlySerializedAs("debug")] [SerializeField] public bool debugTime;

        [NonSerialized] public BehaviourTree Tree;

        private bool _started = false;
        private bool _aborted = false;
        private NodeState _state = NodeState.Running;

        public virtual string Description => "节点";

        private string Tag
        {
            get
            {
                if (string.IsNullOrEmpty(comment))
                {
#if UNITY_EDITOR
                    return $"{GetType().Name}({guid})";
#else
                    return $"{GetType().Name}";
#endif
                }

                return $"{GetType().Name}({comment})";
            }
        }

        public bool Started => _started;
        public bool Aborted => _aborted;
        public NodeState State => _state;
        public bool StopWhenAbort => stopWhenAbort;

        /// <summary>
        /// 节点重置函数，用于重置节点运行态以及运行数据
        /// </summary>
        public void Reset(object payload)
        {
            _started = false;
            if (debugLifecycle)
            {
                DebugUtil.LogCyan($"Node {Tag} Reset");
            }
        }

        /// <summary>
        /// 节点执行函数
        /// </summary>
        public NodeState Tick(float deltaTime, object payload)
        {
            var startTime = Time.realtimeSinceStartup;

            var tickDeltaTime = deltaTime;

            // 执行时将打断复位
            if (_aborted)
            {
                _aborted = false;
                if (debugLifecycle)
                {
                    DebugUtil.LogCyan($"Node {Tag} Resume");
                }

                tickDeltaTime = 0f;
                OnResume(payload);
            }

            if (!_started)
            {
                _started = true;
                if (debugLifecycle)
                {
                    DebugUtil.LogCyan($"Node {Tag} Start");
                }

                tickDeltaTime = 0f;
                OnStart(payload);
            }

            if (debugLifecycle)
            {
                DebugUtil.LogCyan($"Node {Tag} Tick");
            }

            _state = OnTick(tickDeltaTime, payload);

            if (_state == NodeState.Failure || _state == NodeState.Success)
            {
                _started = false;
                _aborted = false;
                if (debugLifecycle)
                {
                    DebugUtil.LogCyan($"Node {Tag} Stop");
                }

                OnStop(payload);
            }

            var endTime = Time.realtimeSinceStartup;
            if (debugTime)
            {
                DebugUtil.LogGrey($"Node {Tag} TickTime: {(endTime - startTime) * 1000}ms");
            }

            return _state;
        }

        /// <summary>
        /// 节点打断函数，打断后节点存在打断但不停止执行（保留状态）或是停止执行（清除状态）两种状态
        /// </summary>
        public void Abort(object payload)
        {
            if (!_started)
            {
                return;
            }

            if (_aborted)
            {
                if (debugLifecycle)
                {
                    DebugUtil.LogCyan($"Node {Tag} Already Aborted");
                }

                return;
            }

            _aborted = true;
            if (debugLifecycle)
            {
                DebugUtil.LogCyan($"Node {Tag} Abort");
            }

            OnAbort(payload);

            // 如果设置为打断即停止，那么下次进入节点时会重新开始，否则下次进入节点时会从上次打断的进度重新开始
            if (stopWhenAbort)
            {
                _started = false;
                _aborted = false;
                _state = NodeState.Failure;
                if (debugLifecycle)
                {
                    DebugUtil.LogCyan($"Node {Tag} Stop");
                }

                OnStop(payload);
            }
        }

        /// <summary>
        /// 克隆函数，用于多个组件同时执行同一文件的行为树时克隆数据
        /// </summary>
        /// <returns></returns>
        public virtual Node Clone()
        {
            return Instantiate(this);
        }

#if UNITY_EDITOR
        /// <summary>
        /// 添加子节点
        /// </summary>
        public virtual bool AddChildNode(Node child)
        {
            return false;
        }

        /// <summary>
        /// 删除子节点
        /// </summary>
        public virtual bool RemoveChildNode(Node child)
        {
            return false;
        }
#endif

        /// <summary>
        /// 节点开始执行函数
        /// </summary>
        protected virtual void OnStart(object payload)
        {
        }

        /// <summary>
        /// 节点恢复执行函数，是在打断后再次调用Tick函数时才会执行
        /// </summary>
        protected virtual void OnResume(object payload)
        {
        }

        /// <summary>
        /// 节点实际执行函数
        /// </summary>
        protected virtual NodeState OnTick(float deltaTime, object payload)
        {
            return NodeState.Failure;
        }

        /// <summary>
        /// 节点打断执行函数
        /// </summary>
        protected virtual void OnAbort(object payload)
        {
        }

        /// <summary>
        /// 节点停止执行函数
        /// </summary>
        protected virtual void OnStop(object payload)
        {
        }
    }
}