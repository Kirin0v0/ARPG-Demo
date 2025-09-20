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
    public class ActionEffectChannelGroup<T> : ActionChannelGroup<T, ActionChannelInspectorSO> where T : IActionTrack, new()
    {
        protected override GenericDropdownMenu ToShowDropdownMenu()
        {
            var dropdownMenu = new GenericDropdownMenu();
            dropdownMenu.AddItem("添加子通道", false, () =>
            {
                var effectChannel = new ActionEffectChannel<ActionEffectTrack>();
                AddChildChannel(effectChannel);
                var channelData = ActionEditorWindow.Instance.CreateEffectChannelData(
                    GUID.Generate().ToString(), "特效子通道");
                UpdateChildChannel(effectChannel, channelData);
            });
            return dropdownMenu;
        }
    }
}