using System.Collections.Generic;
using Animancer;
using Framework.Common.Debug;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Action.Editor.Track.Features
{
    public class ActionAnimationTrackEditorData : ActionTrackFragmentEditorData
    {
        public TransitionAsset Transition;
        public float Speed;

        public override ActionTrackEditorData CopyTo(int targetTick, float tickTime)
        {
            return new ActionAnimationTrackEditorData
            {
                Name = Name,
                RestrictionStrategy = RestrictionStrategy,
                StartTime = targetTick * tickTime,
                StartTick = targetTick,
                Duration = 1 * tickTime,
                DurationTicks = 1,
                Transition = Transition,
                Speed = Speed
            };
        }
    }

    public class ActionAnimationTrackInspectorSO : ActionTrackFragmentInspectorSO
    {
        [LabelText("动画片段"), Delayed, OnValueChanged("UpdateTransition")]
        public TransitionAsset transition;

        [LabelText("动画速度"), Delayed, OnValueChanged("Update")]
        public float speed;

        private void UpdateTransition()
        {
            name = transition != null ? transition.name : "";
            Update();
        }
    }

    public class ActionAnimationTrack : BaseActionTrack<ActionAnimationTrackInspectorSO>
    {
        public override void Bind(ActionTrackEditorData data)
        {
            base.Bind(data);

            // 检查资源是否是包含一个动画片段的TransitionAsset，不是则弹出错误提示
            if (data is ActionAnimationTrackEditorData actionAnimationTrackEditorData)
            {
                if (actionAnimationTrackEditorData.Transition == null)
                {
                    DebugUtil.LogWarning($"The track({this}) has no transition asset");
                }
                else
                {
                    var animationClips = new List<AnimationClip>();
                    actionAnimationTrackEditorData.Transition.GetAnimationClips(animationClips);
                    if (animationClips.Count == 0 || animationClips.Count > 1)
                    {
                        DebugUtil.LogWarning(
                            $"The TransitionAsset({actionAnimationTrackEditorData.Transition}) of the track ({this}) has no clip or more than one clips");
                    }
                }
            }
        }

        protected override void SynchronizeToInspector(ActionAnimationTrackInspectorSO inspector)
        {
            base.SynchronizeToInspector(inspector);
            if (Data is ActionAnimationTrackEditorData data)
            {
                inspector.transition = data.Transition;
                inspector.speed = data.Speed;
            }
        }

        protected override void SynchronizeToTrackData(ActionAnimationTrackInspectorSO inspector)
        {
            base.SynchronizeToTrackData(inspector);
            if (Data is ActionAnimationTrackEditorData data)
            {
                data.Transition = inspector.transition;
                data.Speed = inspector.speed;
            }
        }
    }
}