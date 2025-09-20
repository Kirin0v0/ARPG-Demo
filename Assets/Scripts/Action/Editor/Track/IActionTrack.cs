using System.Collections.Generic;
using Action.Editor.Channel;
using Action.Editor.Track;
using UnityEngine;
using UnityEngine.UIElements;

namespace Action.Editor.Track
{
    public interface IActionTrack
    {
        // 轨道UI
        string Name { get; }
        IActionChannel Channel { get; }
        ActionTrackEditorData Data { get; }

        // 轨道的生命周期函数
        void Init(VisualElement parent, IActionChannel channel, ActionChannelTrackSupportAbility supportAbility);
        void Bind(ActionTrackEditorData data);
        void Destroy();

        // 轨道的辅助函数
        void Refresh();
        bool Active(float currentTime);
    }
}