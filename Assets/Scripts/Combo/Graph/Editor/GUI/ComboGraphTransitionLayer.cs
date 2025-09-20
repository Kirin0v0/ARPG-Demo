using System;
using System.Collections.Generic;
using System.Linq;
using Combo.Graph.Unit;
using Framework.Common.Debug;
using Framework.Common.Editor.Extension;
using UnityEditor;
using UnityEngine;

namespace Combo.Graph.Editor.GUI
{
    public class ComboGraphTransitionLayer : ComboGraphLayer
    {
        private static Color SelectedColor { get; } = new Color(100, 200, 255, 255) / 255;

        private readonly Dictionary<ComboGraphTransition, (Vector2 from, Vector2 to)> _transitionVertexes = new();

        public ComboGraphTransitionLayer(ComboGraphContext context) : base(context)
        {
        }

        public override void DrawGUI(Rect rect)
        {
            base.DrawGUI(rect);

            _transitionVertexes.Clear();
            Context.TotalTransitions.ForEach(DrawTransition);

            if (Context.InTransitionPreview)
            {
                GUIExtension.Begin();
                if (Context.PreviewToNode == null)
                {
                    GUIExtension.DrawArrowLine(GetTransformRect(Context.PreviewFromNode.rect).center,
                        Event.current.mousePosition, 5, Color.white);
                }
                else
                {
                    GUIExtension.DrawArrowLine(GetTransformRect(Context.PreviewFromNode.rect).center,
                        Context.PreviewToNode.rect.center, 5, Color.white);
                }

                GUIExtension.End();
                Context.Repaint();
            }
        }

        public override void ConsumeEvent()
        {
            base.ConsumeEvent();

            switch (Event.current.type)
            {
                case EventType.MouseDown:
                {
                    HandleTransitionClicked();
                }
                    break;
                case EventType.MouseUp:
                    // 判断是否右键抬起创建菜单
                    if (Event.current.button == 1)
                    {
                        var transition = GetMousePointTransition();
                        if (transition)
                        {
                            CreateTransitionMenu(transition);
                            Event.current.Use();
                        }
                    }

                    break;
                case EventType.KeyDown:
                {
                    if (Event.current.keyCode == KeyCode.Delete)
                    {
                        Context.DeleteSelectedTransition();
                        Event.current.Use();
                    }
                }
                    break;
            }
        }

        private void DrawTransition(ComboGraphTransition transition)
        {
            Rect startRect = GetTransformRect(transition.from.rect);
            Rect endRect = GetTransformRect(transition.to.rect);
            var transitionGroup = Context.TotalTransitions
                .Where(t => (t.from == transition.from && t.to == transition.to) ||
                            (t.from == transition.to && t.to == transition.from)).ToList();
            var index = transitionGroup.FindIndex(t => t == transition);
            if (transitionGroup.Count == 0 || index == -1)
            {
                return;
            }

            Vector2 offset = GetTransitionOffset(startRect.center, endRect.center, transitionGroup.Count, index + 1);
            if (Position.Contains(startRect.center + offset) ||
                Position.Contains(endRect.center + offset) ||
                Position.Contains((endRect.center - startRect.center) * 0.5f + startRect.center + offset))
            {
                var selected = Context.IsTransitionSelected(transition);
                _transitionVertexes.Add(transition, (startRect.center + offset, endRect.center + offset));
                GUIExtension.DrawArrowLine(
                    startRect.center + offset,
                    endRect.center + offset,
                    5,
                    selected ? SelectedColor : transition.Passed ? Color.cyan : Color.white
                );
            }
        }

        private void HandleTransitionClicked()
        {
            var transition = GetMousePointTransition();
            if (transition && EventExtension.IsMouseDown(0))
            {
                if (!Context.IsTransitionSelected(transition))
                {
                    Context.SelectTransition(transition);
                }

                Event.current.Use();
            }
        }

        private void CreateTransitionMenu(ComboGraphTransition transition)
        {
            var genericMenu = new GenericMenu();
            genericMenu.AddItem(new GUIContent("Delete"), false,
                () => { Context.DeleteTransition(transition); });
            genericMenu.ShowAsContext();
        }

        private Vector2 GetTransitionOffset(Vector2 origin, Vector2 target, int count, int index)
        {
            Vector2 direction = target - origin;
            Vector2 offset = Vector2.zero;
            // if (Mathf.Abs(direction.y) > Mathf.Abs(direction.x))
            // {
            //     offset.x += direction.y < 0 ? 10 : -10;
            // }
            // else
            // {
            //     offset.y += direction.x < 0 ? 10 : -10;
            // }

            index = Mathf.Clamp(index, 1, count);
            offset.x += (index - (count + 1) / 2f) * 40f;
            offset.y += (index - (count + 1) / 2f) * 20f;

            return offset * Context.ZoomFactor;
        }

        private ComboGraphTransition GetMousePointTransition()
        {
            foreach (var keyValuePair in _transitionVertexes)
            {
                var fromPosition = keyValuePair.Value.from;
                var toPosition = keyValuePair.Value.to;
                var width = Mathf.Clamp(Mathf.Abs(toPosition.x - fromPosition.x), 10f,
                    Mathf.Abs(toPosition.x - fromPosition.x));
                var height = Mathf.Clamp(Mathf.Abs(toPosition.y - fromPosition.y), 10f,
                    Mathf.Abs(toPosition.y - fromPosition.y));
                var rect = new Rect(0, 0, width, height)
                {
                    center = fromPosition + (toPosition - fromPosition) * 0.5f
                };

                if (rect.MouseOn() && PointNearToLine(fromPosition, toPosition, Event.current.mousePosition, 5f))
                {
                    return keyValuePair.Key;
                }
            }

            return null;
        }

        private bool PointNearToLine(Vector2 start, Vector2 end, Vector2 point, float minDistance)
        {
            Vector2 direction = end - start;
            Vector2 start2point = point - start;
            Vector2 projectDir = start2point.magnitude * Vector2.Dot(direction.normalized, start2point.normalized) *
                                 direction.normalized;
            Vector2 pointProject = start + projectDir;
            float distance = Vector3.Distance(pointProject, point);
            if (distance < minDistance)
            {
                return true;
            }

            return false;
        }
    }
}