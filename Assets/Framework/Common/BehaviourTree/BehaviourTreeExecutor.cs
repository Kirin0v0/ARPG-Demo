using System;
using Framework.Common.BehaviourTree.Node;
using Framework.Common.BehaviourTree.Node.Action;
using Framework.Common.BehaviourTree.Node.Decorator;
using UnityEngine;
using UnityEngine.Serialization;

namespace Framework.Common.BehaviourTree
{
    public class BehaviourTreeExecutor : MonoBehaviour
    {
        [FormerlySerializedAs("tree")] [SerializeField]
        private BehaviourTree treeTemplate; // 这是预设行为树

        private BehaviourTree _runtimeTree; // 这个才是真正运行时的行为树
#if UNITY_EDITOR
        public BehaviourTree RuntimeTree => _runtimeTree;
#endif

        public virtual void Init()
        {
            // 将预设行为树克隆为运行时行为树
            _runtimeTree = treeTemplate.Clone();
        }

        public virtual void Destroy()
        {
            // 销毁运行时行为树
            if (_runtimeTree)
            {
                _runtimeTree.Destroy();
                _runtimeTree = null;
            }
        }

        public void UseBlackboard(Action<Blackboard.Blackboard> callback)
        {
            if (!_runtimeTree)
            {
                return;
            }

            callback.Invoke(_runtimeTree.blackboard);
        }

        public void Dfs(Action<Node.Node> visitor)
        {
            if (!_runtimeTree)
            {
                return;
            }

            _runtimeTree.Dfs(_runtimeTree.rootNode, visitor);
        }

        public virtual TreeState Tick(float deltaTime, object payload = null)
        {
            if (!_runtimeTree)
            {
                return TreeState.Failure;
            }

            return _runtimeTree.Tick(_runtimeTree.treeState == TreeState.Running ? deltaTime : 0f, payload);
        }

        public TreeState GetTreeState()
        {
            return _runtimeTree?.treeState ?? TreeState.Failure;
        }
    }
}