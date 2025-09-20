using Skill.Unit;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Skill.Editor.UI.Node
{
    public class SkillFlowTimelineNodeNodeView : SkillFlowNodeView
    {
        public SkillFlowTimelineNodeNodeView(SkillFlowTimelineNodeNode node) : base(node, "Assets/Scripts/Skill/Editor/UI/Node/SkillFlowTimelineNodeNodeView.uxml")
        {
            // 双向绑定
            var tfId = mainContainer.Q<TextField>("tfId");
            tfId.bindingPath = "id";
            tfId.Bind(new SerializedObject(node));
            var ffTime = mainContainer.Q<FloatField>("ffTime");
            ffTime.bindingPath = "timeElapsed";
            ffTime.Bind(new SerializedObject(node));

            // 设置UI风格
            AddToClassList("timelineNode");
        }
    }
}