using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Action.Editor.Channel.UI
{
    public sealed class ActionChannelTemplateUI : BaseActionChannelUI
    {
        protected override string ChannelTemplateAssetPath =>
            "Assets/Scripts/Action/Editor/Channel/UI/ChannelTemplate.uxml";

        private Label _labelChannelName;
        private Button _btnMore;

        private bool _click;

        public override void CreateView(VisualElement parent)
        {
            base.CreateView(parent);
            _labelChannelName = Root.Q<Label>("LabelChannelName");
            _btnMore = Root.Q<Button>("BtnMore");

            _btnMore.clicked += ClickMoreButton;
            Root.RegisterCallback<MouseDownEvent>(HandleMouseDownEvent);
            Root.RegisterCallback<MouseUpEvent>(HandleMouseUpEvent);
            Root.RegisterCallback<MouseOutEvent>(HandleMouseOutEvent);
        }

        public override void BindView(ActionChannelEditorUIData data)
        {
            base.BindView(data);
            _labelChannelName.text = data.Name;
            Root.style.backgroundColor = data.Color;
            _btnMore.visible = data.ShowMoreButton;
        }

        public override void DestroyView()
        {
            base.DestroyView();

            _btnMore.clicked -= ClickMoreButton;
            Root.UnregisterCallback<MouseDownEvent>(HandleMouseDownEvent);
            Root.UnregisterCallback<MouseUpEvent>(HandleMouseUpEvent);
            Root.UnregisterCallback<MouseOutEvent>(HandleMouseOutEvent);
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
                var backgroundColor = Root.style.backgroundColor.value;
                backgroundColor.a = 0.7f;
                Root.style.backgroundColor = backgroundColor;
            }
        }

        private void HandleMouseUpEvent(MouseUpEvent mouseUpEvent)
        {
            if (!_click)
            {
                return;
            }

            _click = false;
            var backgroundColor = Root.style.backgroundColor.value;
            backgroundColor.a = 1f;
            Root.style.backgroundColor = backgroundColor;
        }

        private void HandleMouseOutEvent(MouseOutEvent mouseOutEvent)
        {
            if (!_click)
            {
                return;
            }

            _click = false;
            var backgroundColor = Root.style.backgroundColor.value;
            backgroundColor.a = 1f;
            Root.style.backgroundColor = backgroundColor;
        }
    }
}