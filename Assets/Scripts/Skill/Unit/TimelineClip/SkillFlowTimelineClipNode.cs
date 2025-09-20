using System;
using Framework.Common.Debug;
using Framework.Common.Timeline.Clip;
using Framework.Common.Timeline.Data;
using Framework.Core.Attribute;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Skill.Unit.TimelineClip
{
    public abstract class SkillFlowTimelineClipNode : SkillFlowNode
    {
        public const float TickTime = 1f / 60;

        [Title("时间轴片段配置")] [MinValue(0f)] public float startTime = 0f; // 片段在时间轴中起始时间
        [MinValue(1)] public int totalTicks = 1; // 片段总帧数

        [Title("调试配置")] public bool debug = false;
        public string debugAlias = "";

        public Framework.Common.Timeline.Clip.TimelineClip GetTimelineClip()
        {
            return new TimelineDelegateClip(
                startTime: startTime,
                totalTicks: totalTicks,
                tickTime: TickTime,
                startDelegate: Begin,
                tickDelegate: Tick,
                stopDelegate: End
            );
        }

        private void Begin(Framework.Common.Timeline.Clip.TimelineClip timelineClip,
            TimelineInfo timelineInfo)
        {
            // 调试输出日志
            if (debug)
            {
                var alias = String.IsNullOrEmpty(debugAlias) ? id : debugAlias;
                DebugUtil.LogLightBlue($"{Title}-{alias}: OnBegin");
            }

            OnBegin(timelineClip, timelineInfo);
        }

        private void Tick(Framework.Common.Timeline.Clip.TimelineClip timelineClip,
            TimelineInfo timelineInfo)
        {
            // 调试输出日志
            if (debug)
            {
                var alias = String.IsNullOrEmpty(debugAlias) ? id : debugAlias;
                DebugUtil.LogLightBlue($"{Title}-{alias}: OnTick");
            }

            OnTick(timelineClip, timelineInfo);
        }

        private void End(Framework.Common.Timeline.Clip.TimelineClip timelineClip,
            TimelineInfo timelineInfo)
        {
            // 调试输出日志
            if (debug)
            {
                var alias = String.IsNullOrEmpty(debugAlias) ? id : debugAlias;
                DebugUtil.LogLightBlue($"{Title}-{alias}: OnEnd");
            }

            OnEnd(timelineClip, timelineInfo);
        }

        /// <summary>
        /// TimelineClip类的Start函数的委托函数
        /// </summary>
        /// <param name="timelineClip"></param>
        /// <param name="timelineInfo"></param>
        protected abstract void OnBegin(Framework.Common.Timeline.Clip.TimelineClip timelineClip,
            TimelineInfo timelineInfo);

        /// <summary>
        /// TimelineClip类的Tick函数的委托函数
        /// </summary>
        /// <param name="timelineClip"></param>
        /// <param name="timelineInfo"></param>
        protected abstract void OnTick(Framework.Common.Timeline.Clip.TimelineClip timelineClip,
            TimelineInfo timelineInfo);

        /// <summary>
        /// TimelineClip类的Stop函数的委托函数
        /// </summary>
        /// <param name="timelineClip"></param>
        /// <param name="timelineInfo"></param>
        protected abstract void OnEnd(Framework.Common.Timeline.Clip.TimelineClip timelineClip,
            TimelineInfo timelineInfo);
    }
}