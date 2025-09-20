using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Action.Editor.Channel.UI
{
    public class ActionTrackChannelTemplateUI : BaseActionChannelUI
    {
        public Func<Object, float, bool> AllowDragToTrackChannel;
        public Action<Object, float> DragToTrackChannel;

        protected override string ChannelTemplateAssetPath =>
            "Assets/Scripts/Action/Editor/Channel/UI/TrackChannelTemplate.uxml";

        public override void CreateView(VisualElement parent)
        {
            base.CreateView(parent);

            Root.RegisterCallback<DragUpdatedEvent>(HandleDragUpdatedEvent);
            Root.RegisterCallback<DragExitedEvent>(HandleDragExitedEvent);
        }

        public override void BindView(ActionChannelEditorUIData data)
        {
            base.BindView(data);
            Root.style.backgroundColor = data.Color;
        }

        public override void DestroyView()
        {
            base.DestroyView();

            Root.UnregisterCallback<DragUpdatedEvent>(HandleDragUpdatedEvent);
            Root.UnregisterCallback<DragExitedEvent>(HandleDragExitedEvent);
        }

        private void HandleDragUpdatedEvent(DragUpdatedEvent dragUpdatedEvent)
        {
            if (AllowDragToTrackChannel == null || DragAndDrop.objectReferences.Length == 0)
            {
                return;
            }

            if (AllowDragToTrackChannel.Invoke(DragAndDrop.objectReferences[0], dragUpdatedEvent.localMousePosition.x))
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
            }
        }

        private void HandleDragExitedEvent(DragExitedEvent dragExitedEvent)
        {
            if (AllowDragToTrackChannel == null || DragAndDrop.objectReferences.Length == 0)
            {
                return;
            }

            if (AllowDragToTrackChannel.Invoke(DragAndDrop.objectReferences[0], dragExitedEvent.localMousePosition.x))
            {
                DragToTrackChannel?.Invoke(DragAndDrop.objectReferences[0], dragExitedEvent.localMousePosition.x);
            }
        }
    }
}