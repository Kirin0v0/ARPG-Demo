using System;
using System.Collections.Generic;
using Action.Editor.Track.UI;
using Framework.Common.Debug;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Action.Editor.Track.UI
{
    public sealed class ActionTrackTemplateUI : BaseActionTrackUI
    {
        public Func<GenericDropdownMenu> ToShowDropdownMenu;
        public Action<bool> OnTrackSelected;
        public Func<int, bool> MoveToTargetFrame;

        protected override string TrackTemplateAssetPath =>
            "Assets/Scripts/Action/Editor/Track/UI/TrackTemplate.uxml";

        private Label _labelTrackName;

        private bool _selected;
        private bool _drag;

        public override void CreateView(VisualElement parent)
        {
            base.CreateView(parent);
            _labelTrackName = Root.Q<Label>("LabelTrackName");

            Root.RegisterCallback<MouseDownEvent>(HandleMouseDownEvent);
            Root.RegisterCallback<MouseMoveEvent>(HandleMouseMoveEvent);
            Root.RegisterCallback<MouseUpEvent>(HandleMouseUpEvent);
            Root.RegisterCallback<MouseOutEvent>(HandleMouseOutEvent);
        }

        public override void BindView(ActionTrackEditorUIData data)
        {
            base.BindView(data);

            _labelTrackName.text = data.Name;
            Root.style.backgroundColor = _selected ? Data.SelectedColor : Data.NormalColor;
            Root.style.width = Data.DurationFrames * ActionEditorWindow.Instance.FrameTimescaleScaleFactor *
                               ActionEditorWindow.StandardFrameTimescaleUnitStepPixel;
            var originPosition = Root.transform.position;
            Root.transform.position = new Vector3(
                Data.StartFrame * ActionEditorWindow.Instance.FrameTimescaleScaleFactor *
                ActionEditorWindow.StandardFrameTimescaleUnitStepPixel,
                originPosition.y,
                originPosition.z
            );
        }

        public override void DestroyView()
        {
            base.DestroyView();

            Root.UnregisterCallback<MouseDownEvent>(HandleMouseDownEvent);
            Root.UnregisterCallback<MouseMoveEvent>(HandleMouseMoveEvent);
            Root.UnregisterCallback<MouseUpEvent>(HandleMouseUpEvent);
            Root.UnregisterCallback<MouseOutEvent>(HandleMouseOutEvent);
        }

        public override void RefreshView()
        {
            if (_drag)
            {
                return;
            }

            Root.style.width = Data.DurationFrames * ActionEditorWindow.Instance.FrameTimescaleScaleFactor *
                               ActionEditorWindow.StandardFrameTimescaleUnitStepPixel;
            var originPosition = Root.transform.position;
            Root.transform.position = new Vector3(
                Data.StartFrame * ActionEditorWindow.Instance.FrameTimescaleScaleFactor * ActionEditorWindow.StandardFrameTimescaleUnitStepPixel,
                originPosition.y,
                originPosition.z
            );
        }

        private void HandleMouseDownEvent(MouseDownEvent mouseDownEvent)
        {
            if (mouseDownEvent.button == 0)
            {
                _selected = true;
                OnTrackSelected?.Invoke(_selected);
                Root.style.backgroundColor = Data.SelectedColor;
                _drag = true;
            }
            else if (mouseDownEvent.button == 1)
            {
                var dropdownMenu = ToShowDropdownMenu?.Invoke();
                dropdownMenu?.DropDown(
                    new Rect(mouseDownEvent.mousePosition.x, mouseDownEvent.mousePosition.y - Root.contentRect.height,
                        30, 30),
                    Root, false);
            }
        }

        private void HandleMouseMoveEvent(MouseMoveEvent mouseMoveEvent)
        {
            if (!_drag)
            {
                return;
            }

            Root.style.width = Data.DurationFrames * ActionEditorWindow.Instance.FrameTimescaleScaleFactor *
                               ActionEditorWindow.StandardFrameTimescaleUnitStepPixel;
            var originPosition = Root.transform.position;
            Root.transform.position = new Vector3(
                originPosition.x + mouseMoveEvent.mouseDelta.x,
                originPosition.y,
                originPosition.z
            );
        }

        private void HandleMouseUpEvent(MouseUpEvent mouseUpEvent)
        {
            if (!_selected)
            {
                return;
            }

            _selected = false;
            OnTrackSelected?.Invoke(_selected);
            Root.style.backgroundColor = Data.NormalColor;

            if (!_drag)
            {
                return;
            }

            Root.style.width = Data.DurationFrames * ActionEditorWindow.Instance.FrameTimescaleScaleFactor *
                               ActionEditorWindow.StandardFrameTimescaleUnitStepPixel;
            var originPosition = Root.transform.position;
            Root.transform.position = new Vector3(
                originPosition.x + mouseUpEvent.mouseDelta.x,
                originPosition.y,
                originPosition.z
            );

            _drag = false;
            var actionEditorWindow = ActionEditorWindow.Instance;
            var frame = actionEditorWindow.CalculateTimescalePositionNearFrame(Root.transform.position.x, false);
            if (MoveToTargetFrame?.Invoke(frame) != true)
            {
                // 这里手动恢复视图
                RefreshView();
            }
        }

        private void HandleMouseOutEvent(MouseOutEvent mouseOutEvent)
        {
            if (!_selected)
            {
                return;
            }

            _selected = false;
            OnTrackSelected?.Invoke(_selected);
            Root.style.backgroundColor = Data.NormalColor;

            if (!_drag)
            {
                return;
            }

            Root.style.width = Data.DurationFrames * ActionEditorWindow.Instance.FrameTimescaleScaleFactor *
                               ActionEditorWindow.StandardFrameTimescaleUnitStepPixel;
            var originPosition = Root.transform.position;
            Root.transform.position = new Vector3(
                originPosition.x + mouseOutEvent.mouseDelta.x,
                originPosition.y,
                originPosition.z
            );

            _drag = false;
            var actionEditorWindow = ActionEditorWindow.Instance;
            var frame = actionEditorWindow.CalculateTimescalePositionNearFrame(Root.transform.position.x, false);
            if (MoveToTargetFrame?.Invoke(frame) != true)
            {
                // 这里手动恢复视图
                RefreshView();
            }
        }
    }
}