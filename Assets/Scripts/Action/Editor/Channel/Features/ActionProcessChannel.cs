using Action.Editor.Track;
using UnityEngine;
using UnityEngine.UIElements;

namespace Action.Editor.Channel.Features
{
    public class ActionProcessChannel<T> : ActionChannel<T, ActionChannelInspectorSO> where T : IActionTrack, new()
    {
    }
}