using System;
using Action.Editor.Channel.UI;
using Action.Editor.Track;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Action.Editor.Channel.UI
{
    public abstract class BaseActionChannelUI : IActionChannelUI
    {
        protected Func<GenericDropdownMenu> ToShowDropdownMenu;
        protected System.Action ToClickChannel;

        public VisualElement Root { get; private set; }
        public VisualElement Parent { get; private set; }
        public ActionChannelEditorUIData Data { get; private set; }

        protected virtual string ChannelTemplateAssetPath => "";

        public virtual void CreateView(VisualElement parent)
        {
            Parent = parent;
            Root = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(ChannelTemplateAssetPath).CloneTree().Query()
                .ToList()[1];
            parent.Add(Root);
        }

        public virtual void BindView(ActionChannelEditorUIData data)
        {
            Data = data;
        }

        public virtual void DestroyView()
        {
            Parent?.Remove(Root);
            Parent = null;
        }

        public void SetDropdownMenu(Func<GenericDropdownMenu> showDropdownMenu)
        {
            ToShowDropdownMenu = showDropdownMenu;
        }

        public void SetClickChannelCallback(System.Action callback)
        {
            ToClickChannel = callback;
        }
    }
}