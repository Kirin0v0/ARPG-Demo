using Action.Editor.Channel.Extension;
using Action.Editor.Track;
using Action.Editor.Track.Extension;
using Animancer;
using Animancer.TransitionLibraries;
using Framework.Common.Debug;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UIElements;

namespace Action.Editor.Channel.Features
{
    public class ActionAnimationChannel<T> : ActionChannel<T, ActionChannelInspectorSO>
        where T : IActionTrack, new()
    {
        public override void Init([CanBeNull] IActionChannelGroup group, VisualElement channelParent,
            VisualElement trackChannelParent)
        {
            base.Init(group, channelParent, trackChannelParent);
            TrackChannelTemplateUI.AllowDragToTrackChannel = AllowDragToTrackChannel;
            TrackChannelTemplateUI.DragToTrackChannel = DragToTrackChannel;
        }

        public override void Destroy()
        {
            TrackChannelTemplateUI.AllowDragToTrackChannel = null;
            TrackChannelTemplateUI.DragToTrackChannel = null;
            base.Destroy();
        }

        protected override GenericDropdownMenu ToShowDropdownMenu()
        {
            var dropdownMenu = new GenericDropdownMenu();
            dropdownMenu.AddItem("删除通道", false, () =>
            {
                if (Group == null)
                {
                    return;
                }

                Group.RemoveChildChannel(this);
            });
            return dropdownMenu;
        }

        private bool AllowDragToTrackChannel(Object obj, float mousePositionX)
        {
            // 过滤非动画文件
            if (obj is not TransitionAsset transitionAsset)
            {
                return false;
            }

            // 判断通道数据
            if (Group != null && Group.Data is ActionAnimationChannelGroupEditorData animationChannelEditorData)
            {
                // 如果通道未配置动画库，则不允许拖拽
                if (!animationChannelEditorData.TransitionLibraryAsset)
                {
                    DebugUtil.LogWarning(
                        $"The channel({Data.Name}) does not have a transition library, please bind a transition library to the channel");
                    return false;
                }

                // 如果通道动画库不包含拖曳的动画，则不允许拖拽
                if (!animationChannelEditorData.TransitionLibraryAsset.Library.ContainsKey(transitionAsset.Key))
                {
                    DebugUtil.LogWarning(
                        $"The transition({transitionAsset.name}) does not belong to the transition library({animationChannelEditorData.TransitionLibraryAsset.name}) of the channel({Data.Name})");
                    return false;
                }
            }

            var nearFrame = ActionEditorWindow.Instance.CalculateTimescalePositionNearFrame(mousePositionX, false);
            var trackData = ActionEditorWindow.Instance.CreateAnimationTrackData(transitionAsset, nearFrame);
            if (!this.HasTimeCollisionWithTarget(trackData)
                && this.AllowMoveToTargetFrame(trackData, nearFrame, ActionEditorWindow.Instance.TotalFrames))
            {
                return true;
            }

            return false;
        }

        private void DragToTrackChannel(Object obj, float mousePositionX)
        {
            if (obj is TransitionAsset transitionAsset)
            {
                var nearFrame = ActionEditorWindow.Instance.CalculateTimescalePositionNearFrame(mousePositionX, false);
                var trackData = ActionEditorWindow.Instance.CreateAnimationTrackData(transitionAsset, nearFrame);
                var track = new T();
                BindTrack(track, trackData);
            }
        }
    }
}