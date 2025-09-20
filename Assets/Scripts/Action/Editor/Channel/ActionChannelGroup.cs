using System.Collections.Generic;
using Action.Editor.Channel.UI;
using Action.Editor.Track;
using Framework.Common.Debug;
using JetBrains.Annotations;
using UnityEngine.UIElements;

namespace Action.Editor.Channel
{
    public abstract class ActionChannelGroup<T, Inspector> : BaseActionChannel<T, Inspector>, IActionChannelGroup
        where T : IActionTrack, new()
        where Inspector : ActionChannelInspectorSO
    {
        public override IActionChannelUI ChannelUI => ChannelGroupTemplateUI;
        public override IActionChannelUI TrackChannelUI => TrackChannelGroupTemplateUI;

        protected readonly ActionChannelGroupTemplateUI ChannelGroupTemplateUI = new();
        protected readonly ActionTrackChannelGroupTemplateUI TrackChannelGroupTemplateUI = new();

        public readonly List<IActionChannel> ChildChannels = new();

        public override void Init([CanBeNull] IActionChannelGroup group, VisualElement channelParent,
            VisualElement trackChannelParent)
        {
            base.Init(group, channelParent, trackChannelParent);
            ChildChannels.Clear();
        }

        public override void Destroy()
        {
            base.Destroy();

            // 销毁子通道
            for (var i = ChildChannels.Count - 1; i >= 0; i--)
            {
                var childChannel = ChildChannels[i];
                childChannel.Destroy();
                ChildChannels.RemoveAt(i);
            }
        }

        public void AddChildChannel(IActionChannel channel)
        {
            channel.Init(this, ChannelGroupTemplateUI.ChildRoot, TrackChannelGroupTemplateUI.ChildRoot);
            ChildChannels.Add(channel);
        }

        public void UpdateChildChannel(IActionChannel channel, ActionChannelEditorData data)
        {
            if (ChildChannels.Contains(channel))
            {
                channel.Bind(data);
            }
            else
            {
                AddChildChannel(channel);
                channel.Bind(data);
            }
        }

        public void RemoveChildChannel(IActionChannel channel)
        {
            if (ChildChannels.Remove(channel))
            {
                channel.Destroy();
            }
        }
    }
}