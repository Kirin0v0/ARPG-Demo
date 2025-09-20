using Framework.Common.BehaviourTree.Node;
using Framework.Common.BehaviourTree.Node.Action;
using Framework.Common.BehaviourTree.Node.Composite;
using Framework.Common.BehaviourTree.Node.Decorator;
using Framework.Common.Debug;
using Skill.Unit;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Skill.Editor.UI.Node
{
    public class SkillFlowRootNodeView : SkillFlowNodeView
    {
        public SkillFlowRootNodeView(SkillFlowRootNode node) : base(node,
                "Assets/Scripts/Skill/Editor/UI/Node/SkillFlowRootNodeView.uxml")
        {
            Input.portCapLit = true;
            
            // 双向绑定
            var tfId = mainContainer.Q<TextField>("tfId");
            tfId.bindingPath = "id";
            tfId.Bind(new SerializedObject(node));
            var tfName = mainContainer.Q<TextField>("tfName");
            tfName.bindingPath = "name";
            tfName.Bind(new SerializedObject(node));

            // 设置UI风格
            AddToClassList("root");
        }

        internal void SortChildren()
        {
            if (Node is SkillFlowRootNode rootNode)
            {
                rootNode.SortChildTimelines();
            }
        }
    }
}