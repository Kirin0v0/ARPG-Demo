using Animancer;
using Animancer.TransitionLibraries;
using Framework.Common.BehaviourTree.Node;
using Framework.Common.Debug;
using Framework.Common.Timeline.Data;
using Sirenix.OdinInspector;

namespace Skill.Unit.TimelineClip
{
    [NodeMenuItem("Timeline Clip/Animation")]
    public class SkillFlowTimelineClipAnimationNode : SkillFlowTimelineClipNode
    {
        public enum TargetType
        {
            Caster,
            Target,
        }

        [Title("动画配置")] public bool useStringAsset = false;
        [ShowIf("@useStringAsset")] public StringAsset animationStringAsset;
        [ShowIf("@!useStringAsset")] public TransitionAsset animationTransition;
        public TargetType targetType = TargetType.Caster;

        private AnimancerState _animancerState;

        public override string Title => "时间轴片段——动画节点";

        protected override void OnBegin(Framework.Common.Timeline.Clip.TimelineClip timelineClip,
            TimelineInfo timelineInfo)
        {
            switch (targetType)
            {
                case TargetType.Caster:
                {
                    if (!Caster)
                    {
                        DebugUtil.LogError("The caster is not existing while targetType is Caster");
                        return;
                    }

                    _animancerState = useStringAsset
                        ? Caster.AnimationAbility?.PlayAction(animationStringAsset)
                        : Caster.AnimationAbility?.PlayAction(animationTransition);
                }
                    break;
                case TargetType.Target:
                {
                    if (!Target)
                    {
                        DebugUtil.LogError("The target is not existing while targetType is Target");
                        return;
                    }

                    _animancerState = useStringAsset
                        ? Target.AnimationAbility?.PlayAction(animationStringAsset)
                        : Target.AnimationAbility?.PlayAction(animationTransition);
                }
                    break;
            }
        }

        protected override void OnTick(Framework.Common.Timeline.Clip.TimelineClip timelineClip,
            TimelineInfo timelineInfo)
        {
        }

        protected override void OnEnd(Framework.Common.Timeline.Clip.TimelineClip timelineClip,
            TimelineInfo timelineInfo)
        {
            if (_animancerState != null)
            {
                switch (targetType)
                {
                    case TargetType.Caster:
                    {
                        if (!Caster)
                        {
                            DebugUtil.LogError("The caster is not existing while targetType is Caster");
                            return;
                        }

                        Caster.AnimationAbility?.StopAction(_animancerState);
                    }
                        break;
                    case TargetType.Target:
                    {
                        if (!Target)
                        {
                            DebugUtil.LogError("The target is not existing while targetType is Target");
                            return;
                        }

                        Target.AnimationAbility?.StopAction(_animancerState);
                    }
                        break;
                }

                _animancerState = null;
            }
        }
    }
}