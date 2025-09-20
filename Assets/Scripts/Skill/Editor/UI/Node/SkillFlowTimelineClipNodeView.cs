using Skill.Unit.TimelineClip;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Skill.Editor.UI.Node
{
    public class SkillFlowTimelineClipNodeView : SkillFlowNodeView
    {
        public SkillFlowTimelineClipNodeView(SkillFlowTimelineClipNode node) : base(node, "Assets/Scripts/Skill/Editor/UI/Node/SkillFlowTimelineClipNodeView.uxml")
        {
            // 双向绑定
            var tfId = mainContainer.Q<TextField>("tfId");
            tfId.bindingPath = "id";
            tfId.Bind(new SerializedObject(node));
            var ffStartTime = mainContainer.Q<FloatField>("ffStartTime");
            ffStartTime.bindingPath = "startTime";
            ffStartTime.Bind(new SerializedObject(node));
            var ifTotalTicks = mainContainer.Q<IntegerField>("ifTotalTicks");
            ifTotalTicks.bindingPath = "totalTicks";
            ifTotalTicks.Bind(new SerializedObject(node));

            // 设置UI风格
            AddToClassList("timelineClip");
        }
    }
}