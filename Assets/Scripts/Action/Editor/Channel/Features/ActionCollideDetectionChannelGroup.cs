using System.Collections.Generic;
using Animancer;
using Action.Editor.Track;
using Action.Editor.Track.Features;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Action.Editor.Channel.Features
{
    public class ActionCollideDetectionChannelGroup<T> : ActionChannelGroup<T, ActionChannelInspectorSO> where T : IActionTrack, new()
    {
        protected override GenericDropdownMenu ToShowDropdownMenu()
        {
            var dropdownMenu = new GenericDropdownMenu();
            dropdownMenu.AddItem("添加子通道", false, () =>
            {
                var audioChannel = new ActionCollideDetectionChannel<ActionCollideDetectionTrack>();
                AddChildChannel(audioChannel);
                var channelData = ActionEditorWindow.Instance.CreateCollideDetectionChannelData(
                    GUID.Generate().ToString(), "碰撞检测子通道");
                UpdateChildChannel(audioChannel, channelData);
            });
            return dropdownMenu;
        }
    }
}