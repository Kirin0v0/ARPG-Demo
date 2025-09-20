using System.Collections.Generic;
using Action.Editor.Channel.UI;
using Action.Editor.Track;
using JetBrains.Annotations;
using UnityEngine.UIElements;

namespace Action.Editor.Channel
{
    public interface IActionChannel
    {
        // 通道UI
        IActionChannelUI ChannelUI { get; }
        IActionChannelUI TrackChannelUI { get; }
        IActionChannelGroup Group { get; }
        IActionTrack[] Tracks { get; }
        ActionChannelEditorData Data { get; }

        // 通道的生命周期函数
        void Init([CanBeNull] IActionChannelGroup group, VisualElement channelParent, VisualElement trackChannelParent);
        void Bind(ActionChannelEditorData data);
        void Destroy();
        
        // 通道对其轨道的生命周期函数
        void InitTracks(List<ActionTrackEditorData> trackDataList);
        void BindTrack(IActionTrack track, ActionTrackEditorData trackData);
        void UnbindTrack(IActionTrack track);
        
        // 通道对其轨道的支持函数
        bool AllowMoveTrackToPosition(IActionTrack track, int targetFrame); // 返回值为是否可以移动
        bool TryMoveTrackToPosition(IActionTrack track, int targetFrame, out int moveToFrame); // 返回值为是否可以移动，moveToFrame为最终移动到的帧数
    }
}