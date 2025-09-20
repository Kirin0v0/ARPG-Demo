using Action.Editor.Track;
using Action.Editor.Track.Features;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine.UIElements;

namespace Action.Editor.Channel.Features
{
    public class ActionEventChannelGroup<T> : ActionChannelGroup<T, ActionChannelInspectorSO> where T : IActionTrack, new()
    {
        protected override GenericDropdownMenu ToShowDropdownMenu()
        {
            var dropdownMenu = new GenericDropdownMenu();
            dropdownMenu.AddItem("添加子通道", false, () =>
            {
                var audioChannel = new ActionEventChannel<ActionEventTrack>();
                AddChildChannel(audioChannel);
                var channelData = ActionEditorWindow.Instance.CreateEventChannelData(
                    GUID.Generate().ToString(), "事件子通道");
                UpdateChildChannel(audioChannel, channelData);
            });
            return dropdownMenu;
        }
    }
}