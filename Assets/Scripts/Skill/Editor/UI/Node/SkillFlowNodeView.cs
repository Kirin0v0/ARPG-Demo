using System.Collections.Generic;
using Skill.Unit;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Skill.Editor.UI.Node
{
    public class SkillFlowNodeView : UnityEditor.Experimental.GraphView.Node
    {
        public readonly SkillFlowNode Node;

        public Port Input;
        public List<Port> Outputs;
        public System.Action<SkillFlowNode> OnNodeSelected;
        public System.Action<SkillFlowNode> OnNodeUnselected;

        public SkillFlowNodeView(SkillFlowNode node) : base(
            "Assets/Scripts/Skill/Editor/UI/Node/SkillFlowNodeView.uxml")
        {
            Node = node;
            viewDataKey = node.guid;
            style.left = node.position.x;
            style.top = node.position.y;
            
            // 双向绑定
            var tfId = mainContainer.Q<TextField>("tfId");
            tfId.bindingPath = "id";
            tfId.Bind(new SerializedObject(node));
            
            CreatePorts(node);
        }

        public SkillFlowNodeView(SkillFlowNode node, string uiFile) : base(uiFile)
        {
            Node = node;
            viewDataKey = node.guid;
            style.left = node.position.x;
            style.top = node.position.y;
            CreatePorts(node);
        }

        public override void SetPosition(Rect newPos)
        {
            base.SetPosition(newPos);
            Undo.RecordObject(Node, "Skill Flow Node(Set Position)");
            Node.position = new Vector2(newPos.xMin, newPos.yMin);
            EditorUtility.SetDirty(Node);
        }

        public override void OnSelected()
        {
            base.OnSelected();
            OnNodeSelected?.Invoke(Node);
        }

        public override void OnUnselected()
        {
            base.OnUnselected();
            OnNodeUnselected?.Invoke(Node);
        }

        private void CreatePorts(SkillFlowNode node)
        {
            // 创建输入端口
            Input = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Single, typeof(bool));
            Input.portName = node.Title;
            Input.style.flexDirection = FlexDirection.Row;
            inputContainer.Add(Input);

            // 创建输出端口
            Outputs = new List<Port>();
            node.GetOutputs().ForEach(output =>
            {
                var outputPort = InstantiatePort(
                    Orientation.Horizontal,
                    Direction.Output, 
                    output.capacity switch
                    {
                        SkillFlowNodePortCapacity.Single => Port.Capacity.Single,
                        SkillFlowNodePortCapacity.Multiple => Port.Capacity.Multi,
                    },
                    typeof(bool));
                outputPort.portName = output.title;
                outputPort.userData = output.key;
                outputPort.style.flexDirection = FlexDirection.RowReverse;
                outputContainer.Add(outputPort);
                Outputs.Add(outputPort);
            });
        }
    }
}