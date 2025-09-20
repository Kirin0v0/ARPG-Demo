using System;
using System.Linq;
using Framework.Common.BehaviourTree.Node;
using Framework.Common.Debug;
using Framework.Common.Timeline.Data;
using Sirenix.OdinInspector;

namespace Skill.Unit.Feature
{
    [NodeMenuItem("Feature/Remove/Timeline")]
    public class SkillFlowFeatureRemoveTimelineNode : SkillFlowFeatureNode
    {
        [Title("删除配置")] public string timelineNodeId;

        public override string Title => "业务——删除时间轴节点";

        public override ISkillFlowFeaturePayloadsRequire GetPayloadsRequire()
        {
            return SkillFlowFeaturePayloadsRequirements.EmptyPayloads;
        }

        protected override void OnExecute(TimelineInfo timelineInfo, object[] payloads)
        {
            var timelineNode = skillFlow.GetNode(timelineNodeId);
            if (!timelineNode)
            {
                DebugUtil.LogError($"Can't find the Timeline node that matches the specified id({timelineNodeId})");
                return;
            }
            
            if (!TimelineManager.ContainsTimeline(timelineNode.RunningId))
            {
                DebugUtil.LogError($"Can't find the Timeline that matches the specified id({timelineNode.RunningId})");
                return;
            }
            
            TimelineManager.StopTimeline(timelineNode.RunningId);
        }
    }
}