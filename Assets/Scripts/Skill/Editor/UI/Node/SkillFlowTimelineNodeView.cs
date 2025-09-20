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
    public class SkillFlowTimelineNodeView : SkillFlowNodeView
    {
        public SkillFlowTimelineNodeView(SkillFlowTimelineNode node) : base(node, "Assets/Scripts/Skill/Editor/UI/Node/SkillFlowTimelineNodeView.uxml")
        {
            // 双向绑定
            var tfOrder = mainContainer.Q<TextField>("tfOrder");
            tfOrder.bindingPath = "executeOrder";
            tfOrder.Bind(new SerializedObject(node));
            var tfId = mainContainer.Q<TextField>("tfId");
            tfId.bindingPath = "id";
            tfId.Bind(new SerializedObject(node));
            var ffDuration = mainContainer.Q<FloatField>("ffDuration");
            ffDuration.bindingPath = "duration";
            ffDuration.Bind(new SerializedObject(node));

            // 设置UI风格
            AddToClassList("timeline");
        }
    }
}