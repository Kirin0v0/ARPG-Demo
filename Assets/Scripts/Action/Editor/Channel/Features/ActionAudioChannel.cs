using Action.Editor.Channel.Extension;
using Action.Editor.Track;
using Action.Editor.Track.Extension;
using Animancer;
using Animancer.TransitionLibraries;
using Framework.Common.Audio;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UIElements;

namespace Action.Editor.Channel.Features
{
    public class ActionAudioChannel<T> : ActionChannel<T, ActionChannelInspectorSO> where T : IActionTrack, new()
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
            if (obj is AudioClip audioClip)
            {
                var nearFrame = ActionEditorWindow.Instance.CalculateTimescalePositionNearFrame(mousePositionX, false);
                var trackData = ActionEditorWindow.Instance.CreateAudioTrackSpecifiedData(audioClip, nearFrame);
                if (!this.HasTimeCollisionWithTarget(trackData)
                    && this.AllowMoveToTargetFrame(trackData, nearFrame, ActionEditorWindow.Instance.TotalFrames))
                {
                    return true;
                }
            }

            if (obj is AudioClipRandomizer audioClipRandomizer)
            {
                var nearFrame = ActionEditorWindow.Instance.CalculateTimescalePositionNearFrame(mousePositionX, false);
                var trackData = ActionEditorWindow.Instance.CreateAudioTrackRandomData(audioClipRandomizer, nearFrame);
                if (!this.HasTimeCollisionWithTarget(trackData)
                    && this.AllowMoveToTargetFrame(trackData, nearFrame, ActionEditorWindow.Instance.TotalFrames))
                {
                    return true;
                }
            }

            return false;
        }

        private void DragToTrackChannel(Object obj, float mousePositionX)
        {
            if (obj is AudioClip audioClip)
            {
                var nearFrame = ActionEditorWindow.Instance.CalculateTimescalePositionNearFrame(mousePositionX, false);
                var trackData = ActionEditorWindow.Instance.CreateAudioTrackSpecifiedData(audioClip, nearFrame);
                var track = new T();
                BindTrack(track, trackData);
            }

            if (obj is AudioClipRandomizer audioClipRandomizer)
            {
                var nearFrame = ActionEditorWindow.Instance.CalculateTimescalePositionNearFrame(mousePositionX, false);
                var trackData = ActionEditorWindow.Instance.CreateAudioTrackRandomData(audioClipRandomizer, nearFrame);
                var track = new T();
                BindTrack(track, trackData);
            }
        }
    }
}