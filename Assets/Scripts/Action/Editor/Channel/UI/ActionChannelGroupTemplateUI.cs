using System;
using System.Collections.Generic;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;

namespace Action.Editor.Channel.UI
{
    public sealed class ActionChannelGroupTemplateUI : BaseActionChannelUI
    {
        public VisualElement ChildRoot => _listChildChannel;

        protected override string ChannelTemplateAssetPath =>
            "Assets/Scripts/Action/Editor/Channel/UI/ChannelGroupTemplate.uxml";

        private Label _labelChannelGroupName;
        private Button _btnMore;
        private VisualElement _layoutChannelGroup;
        private VisualElement _listChildChannel;

        private bool _click;

        public override void CreateView(VisualElement parent)
        {
            base.CreateView(parent);
            _labelChannelGroupName = Root.Q<Label>("LabelChannelGroupName");
            _btnMore = Root.Q<Button>("BtnMore");
            _layoutChannelGroup = Root.Q<VisualElement>("LayoutChannelGroup");
            _listChildChannel = Root.Q<VisualElement>("ListChildChannel");

            _btnMore.clicked += ClickMoreButton;
            _layoutChannelGroup.RegisterCallback<MouseDownEvent>(HandleMouseDownEvent);
            _layoutChannelGroup.RegisterCallback<MouseUpEvent>(HandleMouseUpEvent);
            _layoutChannelGroup.RegisterCallback<MouseOutEvent>(HandleMouseOutEvent);
        }

        public override void BindView(ActionChannelEditorUIData data)
        {
            base.BindView(data);
            _labelChannelGroupName.text = data.Name;
            _layoutChannelGroup.style.backgroundColor = data.Color;
            _btnMore.visible = data.ShowMoreButton;
        }

        public override void DestroyView()
        {
            base.DestroyView();
            
            _btnMore.clicked -= ClickMoreButton;
            _layoutChannelGroup.UnregisterCallback<MouseDownEvent>(HandleMouseDownEvent);
            _layoutChannelGroup.UnregisterCallback<MouseUpEvent>(HandleMouseUpEvent);
            _layoutChannelGroup.UnregisterCallback<MouseOutEvent>(HandleMouseOutEvent);

            _layoutChannelGroup = null;
            _listChildChannel = null;
        }

        private void ClickMoreButton()
        {
            var dropdownMenu = ToShowDropdownMenu?.Invoke();
            dropdownMenu?.DropDown(
                new Rect(_btnMore.worldTransform.GetPosition().x + 20, _btnMore.worldTransform.GetPosition().y, 0, 0),
                _btnMore, false);
        }

        private void HandleMouseDownEvent(MouseDownEvent mouseDownEvent)
        {
            if (mouseDownEvent.button == 0)
            {
                _click = true;
                ToClickChannel?.Invoke();
                var backgroundColor = _layoutChannelGroup.style.backgroundColor.value;
                backgroundColor.a = 0.7f;
                _layoutChannelGroup.style.backgroundColor = backgroundColor;
            }
        }

        private void HandleMouseUpEvent(MouseUpEvent mouseUpEvent)
        {
            if (!_click)
            {
                return;
            }

            _click = false;
            var backgroundColor = _layoutChannelGroup.style.backgroundColor.value;
            backgroundColor.a = 1f;
            _layoutChannelGroup.style.backgroundColor = backgroundColor;
        }

        private void HandleMouseOutEvent(MouseOutEvent mouseOutEvent)
        {
            if (!_click)
            {
                return;
            }

            _click = false;
            var backgroundColor = _layoutChannelGroup.style.backgroundColor.value;
            backgroundColor.a = 1f;
            _layoutChannelGroup.style.backgroundColor = backgroundColor;
        }
    }
}