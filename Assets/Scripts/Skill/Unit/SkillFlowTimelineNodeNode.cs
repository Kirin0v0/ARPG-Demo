using System;
using System.Collections.Generic;
using Framework.Common.BehaviourTree.Node;
using Framework.Common.Debug;
using Framework.Common.Timeline.Data;
using Framework.Common.Timeline.Node;
using Sirenix.OdinInspector;
using Skill.Unit.Feature;
using UnityEngine;

namespace Skill.Unit
{
    [NodeMenuItem("Timeline Node")]
    public class SkillFlowTimelineNodeNode : SkillFlowNode
    {
        private const string FeaturePort = "feature";

        [Title("时间轴节点配置")] [MinValue(0f)] public float timeElapsed = 0f; // 节点位于时间轴时间位置

        [Title("调试配置")] public bool debug = false;
        public string debugAlias = "";

        public override string Title => "时间轴节点";

#if UNITY_EDITOR

        public override List<SkillFlowNodePort> GetOutputs()
        {
            return new List<SkillFlowNodePort>()
            {
                new SkillFlowNodePort
                {
                    key = FeaturePort,
                    title = "业务列表",
                    capacity = SkillFlowNodePortCapacity.Multiple,
                }
            };
        }

        public override bool AddChildNode(string key, SkillFlowNode child)
        {
            if (key == FeaturePort && child is SkillFlowFeatureNode featureNode &&
                featureNode.GetPayloadsRequire() is SkillFlowFeatureNonPayloadsRequire)
            {
                AddChildNodeInternal(key, child);
                return true;
            }

            return false;
        }

        public override bool RemoveChildNode(string key, SkillFlowNode child)
        {
            if (key == FeaturePort && child is SkillFlowFeatureNode featureNode)
            {
                return RemoveChildNodeInternal(key, featureNode);
            }

            return false;
        }
#endif

        public Framework.Common.Timeline.Node.TimelineNode GetTimelineNode()
        {
            return new TimelineDelegateNode(timeElapsed, Execute);
        }

        private void Execute(TimelineInfo timelineInfo)
        {
            // 调试输出日志
            if (debug)
            {
                var alias = String.IsNullOrEmpty(debugAlias) ? id : debugAlias;
                DebugUtil.LogLightBlue($"{Title}-{alias}: OnExecute");
            }

            GetChildNodes(FeaturePort).ForEach(childNode =>
            {
                if (childNode is SkillFlowFeatureNode featureNode &&
                    featureNode.GetPayloadsRequire() is SkillFlowFeatureNonPayloadsRequire nonPayloadsRequire)
                {
                    featureNode.Execute(timelineInfo, nonPayloadsRequire.ProvideContext());
                }
            });
        }
    }
}