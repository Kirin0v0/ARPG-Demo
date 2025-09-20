using System.Collections.Generic;
using Action.Editor.Channel.UI;
using Action.Editor.Track;
using Framework.Common.Debug;
using UnityEngine;
using UnityEngine.UIElements;

namespace Action.Editor.Channel
{
    public abstract class ActionChannel<T, Inspector> : BaseActionChannel<T, Inspector> 
        where T : IActionTrack, new()
        where Inspector : ActionChannelInspectorSO
    {
        public override IActionChannelUI ChannelUI => ChannelTemplateUI;
        public override IActionChannelUI TrackChannelUI => TrackChannelTemplateUI;

        protected readonly ActionChannelTemplateUI ChannelTemplateUI = new();
        protected readonly ActionTrackChannelTemplateUI TrackChannelTemplateUI = new();
    }
}