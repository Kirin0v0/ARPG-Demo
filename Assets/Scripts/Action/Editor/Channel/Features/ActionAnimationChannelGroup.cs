using System.Collections.Generic;
using Animancer;
using Action.Editor.Track;
using Action.Editor.Track.Features;
using Animancer.TransitionLibraries;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Action.Editor.Channel.Features
{
    public class ActionAnimationChannelGroupEditorData : ActionChannelEditorData
    {
        public TransitionLibraryAsset TransitionLibraryAsset;
    }
    
    public class ActionAnimationChannelGroupInspectorSO : ActionChannelInspectorSO
    {
        [LabelText("动画过渡库"), Delayed, OnValueChanged("Update")]
        public TransitionLibraryAsset transitionLibraryAsset;
    }
    
    public class ActionAnimationChannelGroup<T> : ActionChannelGroup<T, ActionAnimationChannelGroupInspectorSO> where T : IActionTrack, new()
    {
        protected override GenericDropdownMenu ToShowDropdownMenu()
        {
            var dropdownMenu = new GenericDropdownMenu();
            dropdownMenu.AddItem("添加子通道", false, () =>
            {
                var animationChannel = new ActionAnimationChannel<ActionAnimationTrack>();
                AddChildChannel(animationChannel);
                var channelData = ActionEditorWindow.Instance.CreateAnimationChannelData(
                    GUID.Generate().ToString(), "动画子通道");
                UpdateChildChannel(animationChannel, channelData);
            });
            return dropdownMenu;
        }
        
        protected override void SynchronizeToInspector(ActionAnimationChannelGroupInspectorSO groupInspector)
        {
            base.SynchronizeToInspector(groupInspector);
            if (Data is ActionAnimationChannelGroupEditorData animationChannelEditorData)
            {
                groupInspector.transitionLibraryAsset = animationChannelEditorData.TransitionLibraryAsset;
            }
        }

        protected override void SynchronizeToTrackData(ActionAnimationChannelGroupInspectorSO groupInspector)
        {
            base.SynchronizeToTrackData(groupInspector);
            if (Data is ActionAnimationChannelGroupEditorData animationChannelEditorData)
            {
                animationChannelEditorData.TransitionLibraryAsset = groupInspector.transitionLibraryAsset;
            }
        }
    }
}