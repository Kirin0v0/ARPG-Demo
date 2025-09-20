using Framework.Common.BehaviourTree.Node;
using Framework.Common.Timeline.Data;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Skill.Unit.TimelineClip
{
    [NodeMenuItem("Timeline Clip/Caster State")]
    public class SkillFlowTimelineClipCasterStateNode : SkillFlowTimelineClipNode
    {
        [Title("状态配置")] [SerializeField] private bool toEndure;
        [SerializeField] private bool toUnbreakable;
        [SerializeField] private bool toImmune;

        public override string Title => "时间轴片段——施法者状态节点";

        protected override void OnBegin(Framework.Common.Timeline.Clip.TimelineClip timelineClip,
            TimelineInfo timelineInfo)
        {
            if (Caster && !Caster.Parameters.dead)
            {
                if (toEndure)
                {
                    Caster.StateAbility.StartEndure(RunningId, float.MaxValue);
                }

                if (toUnbreakable)
                {
                    Caster.StateAbility.StartUnbreakable(RunningId, float.MaxValue);
                }

                if (toImmune)
                {
                    Caster.StateAbility.StartImmune(RunningId,float.MaxValue);
                }
            }
        }

        protected override void OnTick(Framework.Common.Timeline.Clip.TimelineClip timelineClip,
            TimelineInfo timelineInfo)
        {
        }

        protected override void OnEnd(Framework.Common.Timeline.Clip.TimelineClip timelineClip,
            TimelineInfo timelineInfo)
        {
            if (Caster && !Caster.Parameters.dead)
            {
                if (toEndure)
                {
                    Caster.StateAbility.StopEndure(RunningId);
                }

                if (toUnbreakable)
                {
                    Caster.StateAbility.StopUnbreakable(RunningId);
                }

                if (toImmune)
                {
                    Caster.StateAbility.StopImmune(RunningId);
                }
            }
        }
    }
}