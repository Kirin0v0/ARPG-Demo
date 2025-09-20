using Framework.Common.BehaviourTree.Node;
using Framework.Common.Debug;
using Framework.Common.Timeline.Data;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Skill.Unit.Feature
{
    [NodeMenuItem("Feature/Log")]
    public class SkillFlowFeatureLogNode: SkillFlowFeatureNode
    {
        [Title("日志配置")] [TextArea] public string message = "";
        
        public override string Title => "业务——日志节点";

        public override ISkillFlowFeaturePayloadsRequire GetPayloadsRequire()
        {
            return SkillFlowFeaturePayloadsRequirements.EmptyPayloads;
        }

        protected override void OnExecute(TimelineInfo timelineInfo, object[] payloads)
        {
            DebugUtil.LogLightBlue(message);
        }
    }
}