using Framework.Core.Attribute;
using UnityEngine;

namespace Framework.Common.BehaviourTree.Node.Action
{
    [NodeMenuItem("Action/Child Tree")]
    public class ChildTreeNode : ActionNode
    {
        [SerializeField] private BehaviourTree tree; // 这是预设行为树
        [DisplayOnly, SerializeField]  private BehaviourTree runtimeTree; // 这是运行时的行为树

        public override string Description => "子树节点，同步父子树黑板数据，并运行子树流程";

        protected override void OnStart(object payload)
        {
            // 懒加载，等到第一次执行该节点才克隆运行时行为树
            if (!runtimeTree)
            {
                runtimeTree = tree.Clone();
                runtimeTree.Parent = Tree;
            }
        }

        protected override NodeState OnTick(float deltaTime, object payload)
        {
            // 同步父树的黑板数据到子树的黑板上
            blackboard.Synchronize(runtimeTree.blackboard);
            var state = runtimeTree.rootNode.Tick(deltaTime, payload);
            // 在子树运行后同步子树的黑板数据到父树的黑板上
            runtimeTree.rootNode.blackboard.Synchronize(blackboard);
            return state;
        }
        protected override void OnAbort(object payload)
        {
            // 中断子树
            runtimeTree.rootNode.Abort(payload);
        }

        protected override void OnStop(object payload)
        {
            base.OnStop(payload);
        }
    }
}