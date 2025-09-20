using Action.Editor.Channel.Extension;
using Action.Editor.Track;
using Action.Editor.Track.Extension;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UIElements;

namespace Action.Editor.Channel.Features
{
    public class ActionEventChannel<T> : ActionChannel<T, ActionChannelInspectorSO> where T : IActionTrack, new()
    {
        protected override GenericDropdownMenu ToShowDropdownMenu()
        {
            var dropdownMenu = new GenericDropdownMenu();
            dropdownMenu.AddItem("于选中帧起始位置添加子轨道", false, () =>
            {
                var trackData =
                    ActionEditorWindow.Instance.CreateEventTrackData("默认事件", ActionEditorWindow.Instance.SelectedFrame);
                if (this.HasTimeCollisionWithTarget(trackData))
                {
                    return;
                }

                var track = new T();
                BindTrack(track, trackData);
            });
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
    }
}