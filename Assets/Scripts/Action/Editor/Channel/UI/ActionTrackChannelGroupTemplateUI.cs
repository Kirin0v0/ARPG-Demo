using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Action.Editor.Channel.UI
{
    public class ActionTrackChannelGroupTemplateUI : BaseActionChannelUI
    {
        public VisualElement ChildRoot => _listChildTrackChannel;
        
        protected override string ChannelTemplateAssetPath =>
            "Assets/Scripts/Action/Editor/Channel/UI/TrackChannelGroupTemplate.uxml";
        
        private VisualElement _layoutTrackChannelGroup;
        private VisualElement _listChildTrackChannel;

        public override void CreateView(VisualElement parent)
        {
            base.CreateView(parent);
            _layoutTrackChannelGroup = Root.Q<VisualElement>("LayoutTrackChannelGroup");
            _listChildTrackChannel = Root.Q<VisualElement>("ListChildTrackChannel");
        }

        public override void BindView(ActionChannelEditorUIData data)
        {
            base.BindView(data);
            _layoutTrackChannelGroup.style.backgroundColor = data.Color;
        }

        public override void DestroyView()
        {
            base.DestroyView();
            _layoutTrackChannelGroup = null;
            _listChildTrackChannel = null;
        }
    }
}