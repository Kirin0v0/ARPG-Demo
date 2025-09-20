using Action.Editor.Track;

namespace Action.Editor.Channel.Features
{
    public class ActionProcessChannelGroup<T> : ActionChannelGroup<T, ActionChannelInspectorSO> where T : IActionTrack, new()
    {
    }
}