using System;
using System.Collections.Generic;
using Framework.Common.BehaviourTree.Node;
using Framework.Common.Debug;
using Framework.Common.Timeline;
using Framework.Common.Timeline.Data;
using Sirenix.OdinInspector;
using Skill.Unit.TimelineClip;
using UnityEngine;

namespace Skill.Unit
{
    [NodeMenuItem("Timeline")]
    public class SkillFlowTimelineNode : SkillFlowNode
    {
        private const string TimelineNodePort = "timelineNode";

        [HideInInspector] public int executeOrder = 1; // 时间轴执行顺序，从1开始，用于技能时间轴排序

        [Title("时间轴配置")] public float duration;

        [Title("调试配置")] public bool debug = false;
        public string debugAlias = "";

        [NonSerialized] public float TimeScale = 1f; // 时间轴时间缩放，仅在运行时设置

        public override string Title => "时间轴";

#if UNITY_EDITOR

        public override List<SkillFlowNodePort> GetOutputs()
        {
            return new List<SkillFlowNodePort>()
            {
                new SkillFlowNodePort
                {
                    key = TimelineNodePort,
                    title = "时间轴片段、节点列表",
                    capacity = SkillFlowNodePortCapacity.Multiple,
                }
            };
        }

        public override bool AddChildNode(string key, SkillFlowNode child)
        {
            if (key == TimelineNodePort && (child is SkillFlowTimelineClipNode timelineClipNode ||
                                            child is SkillFlowTimelineNodeNode timelineNodeNode))
            {
                AddChildNodeInternal(key, child);
                return true;
            }

            return false;
        }

        public override bool RemoveChildNode(string key, SkillFlowNode child)
        {
            if (key == TimelineNodePort && (child is SkillFlowTimelineClipNode timelineClipNode ||
                                            child is SkillFlowTimelineNodeNode timelineNodeNode))
            {
                return RemoveChildNodeInternal(key, child);
            }

            return false;
        }
#endif

        public void StartTimeline(GameObject caster)
        {
            // 调试输出日志
            if (debug)
            {
                var alias = String.IsNullOrEmpty(debugAlias) ? id : debugAlias;
                DebugUtil.LogLightBlue($"{Title}-{alias}: StartTimeline");
            }

            var timelineInfo = TimelineManager.StartTimeline(GetTimeline(), caster);
            timelineInfo.Timescale = TimeScale;
        }

        private Timeline GetTimeline()
        {
            var timelineClips = new List<Framework.Common.Timeline.Clip.TimelineClip>();
            var timelineNodes = new List<Framework.Common.Timeline.Node.TimelineNode>();
            GetChildNodes(TimelineNodePort).ForEach(childNode =>
            {
                switch (childNode)
                {
                    case SkillFlowTimelineClipNode timelineClipNode:
                        timelineClips.Add(timelineClipNode.GetTimelineClip());
                        break;
                    case SkillFlowTimelineNodeNode timelineNodeNode:
                        timelineNodes.Add(timelineNodeNode.GetTimelineNode());
                        break;
                }
            });
            var timeline = new Timeline
            {
                Id = RunningId,
                Clips = timelineClips.ToArray(),
                Nodes = timelineNodes.ToArray(),
                Duration = duration,
            };
            return timeline;
        }
    }
}