using Framework.Common.BehaviourTree.Node;
using Framework.Common.BehaviourTree.Node.Action;
using Framework.Common.BehaviourTree.Node.Composite;
using Framework.Common.BehaviourTree.Node.Decorator;
using Framework.Common.Debug;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Framework.Common.BehaviourTree.Editor.UI
{
    public class BehaviourTreeNodeView : UnityEditor.Experimental.GraphView.Node
    {
        public readonly Node.Node Node;
        public readonly Port Input;
        public readonly Port Output;

        public System.Action<BehaviourTreeNodeView> OnNodeSelected;
        public System.Action<BehaviourTreeNodeView> OnNodeUnselected;

        private readonly Label _labelAborted;

        public BehaviourTreeNodeView(Node.Node node) : base(
            "Assets/Framework/Common/BehaviourTree/Editor/UI/BehaviourTreeNodeView.uxml")
        {
            Node = node;
            title = node.name;
            viewDataKey = node.guid;
            style.left = node.position.x;
            style.top = node.position.y;

            // 双向绑定
            var labelDescription = mainContainer.Q<Label>("description");
            var labelExecuteOrder = mainContainer.Q<Label>("executeOrder");
            _labelAborted = mainContainer.Q<Label>("aborted");
            labelDescription.text = node.Description;
            labelExecuteOrder.bindingPath = "executeOrder";
            labelExecuteOrder.Bind(new SerializedObject(node));

            #region 创建输入端口

            switch (Node)
            {
                case RootNode:
                    break;
                default:
                    Input = InstantiatePort(Orientation.Vertical, Direction.Input, Port.Capacity.Single, typeof(bool));
                    break;
            }

            if (Input != null)
            {
                Input.portName = "";
                Input.style.flexDirection = FlexDirection.Column;
                inputContainer.Add(Input);
            }

            #endregion

            #region 创建输出端口

            switch (Node)
            {
                case RootNode rootNode:
                case DecoratorNode decoratorNode:
                    Output = InstantiatePort(Orientation.Vertical, Direction.Output, Port.Capacity.Single,
                        typeof(bool));
                    break;
                case CompositeNode compositeNode:
                    Output = InstantiatePort(Orientation.Vertical, Direction.Output, Port.Capacity.Multi, typeof(bool));
                    break;
            }

            if (Output != null)
            {
                Output.portName = "";
                Output.style.flexDirection = FlexDirection.ColumnReverse;
                outputContainer.Add(Output);
            }

            #endregion

            SetupClasses();
        }

        public override void SetPosition(Rect newPos)
        {
            base.SetPosition(newPos);
            Undo.RecordObject(Node, "Behaviour Tree Node(Set Position)");
            Node.position = new Vector2(newPos.xMin, newPos.yMin);
            EditorUtility.SetDirty(Node);
        }

        public override void OnSelected()
        {
            base.OnSelected();
            OnNodeSelected?.Invoke(this);
        }

        public override void OnUnselected()
        {
            base.OnUnselected();
            OnNodeUnselected?.Invoke(this);
        }

        internal void SortChildren()
        {
            if (Node is CompositeNode compositeNode)
            {
                compositeNode.children.Sort((leftNode, rightNode) =>
                    leftNode.position.x <= rightNode.position.x ? -1 : 1);
                // 子节点执行顺序重新设置
                for (var i = 0; i < compositeNode.children.Count; i++)
                {
                    var childNode = compositeNode.children[i];
                    childNode.executeOrder = i + 1;
                }
            }
        }

        internal void UpdateState()
        {
            if (Application.isPlaying)
            {
                RemoveFromClassList("running");
                RemoveFromClassList("success");
                RemoveFromClassList("failure");

                _labelAborted.style.display = Node.Aborted ? DisplayStyle.Flex : DisplayStyle.None;

                switch (Node.State)
                {
                    case NodeState.Running:
                        if (!Node.Started)
                        {
                            return;
                        }

                        AddToClassList("running");
                        break;
                    case NodeState.Success:
                        AddToClassList("success");
                        break;
                    case NodeState.Failure:
                        AddToClassList("failure");
                        break;
                }
            }
        }

        private void SetupClasses()
        {
            switch (Node)
            {
                case RootNode:
                    AddToClassList("root");
                    break;
                case ActionNode:
                    AddToClassList("action");
                    break;
                case DecoratorNode:
                    AddToClassList("decorator");
                    break;
                case CompositeNode:
                    AddToClassList("composite");
                    break;
            }
        }
    }
}