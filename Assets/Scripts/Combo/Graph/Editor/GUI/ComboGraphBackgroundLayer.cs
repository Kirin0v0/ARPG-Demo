using System.Collections.Generic;
using Combo.Graph.Unit;
using Framework.Common.Debug;
using Framework.Common.Editor.Extension;
using UnityEditor;
using UnityEngine;

namespace Combo.Graph.Editor.GUI
{
    public class ComboGraphBackgroundLayer : ComboGraphLayer
    {
        private static float SmallGridLength => 30f;
        private static float BigGridLength => 300f;
        private static Color GridColor => new Color(0, 0, 0, 0.2f);
        private static Color BackgroundColor => new Color(0, 0, 0, 0.25f);
        private static int NodeWidth => 480;
        private static int NodeHeight => 90;

        private Vector2 _mousePosition;

        public ComboGraphBackgroundLayer(ComboGraphContext context) : base(context)
        {
        }

        public override void DrawGUI(Rect rect)
        {
            base.DrawGUI(rect);

            // 每帧绘制时检查Entry和Exit是否存在，不存在就去创建
            if (Context.ComboGraph)
            {
                if (!Context.ComboGraph.entry)
                {
                    var nodePosition = new Rect(0, 0, NodeWidth, NodeHeight);
                    var node = Context.CreateEntryNode();
                    if (!node)
                    {
                        return;
                    }

                    node.rect = nodePosition;
                    Context.ComboGraph.entry = node;
                }

                if (!Context.ComboGraph.exit)
                {
                    var nodePosition = new Rect(2000, 0, NodeWidth, NodeHeight);
                    var node = Context.CreateExitNode();
                    if (!node)
                    {
                        return;
                    }

                    node.rect = nodePosition;
                    Context.ComboGraph.exit = node;
                }
            }

            if (Event.current.type == EventType.Repaint)
            {
                EditorGUI.DrawRect(rect, BackgroundColor);
                DrawGrid(rect, SmallGridLength, GridColor);
                DrawGrid(rect, BigGridLength, GridColor);
            }
        }

        public override void ConsumeEvent()
        {
            base.ConsumeEvent();

            _mousePosition = Event.current.mousePosition;
            switch (Event.current.type)
            {
                case EventType.MouseDown:
                {
                    // 取消选中
                    if (Position.MouseOn() && Event.current.button == 0)
                    {
                        Context.UnselectNodes();
                        Context.UnselectTransition();
                        Context.ClearPreviewTransition();
                    }
                }
                    break;
                case EventType.MouseUp:
                {
                    // 右键菜单
                    if (Position.MouseOn() && !IsMouseOverAnyNode() &&
                        Event.current.button == 1 && Context.ComboGraph != null)
                    {
                        Context.UnselectNodes();
                        Context.UnselectTransition();
                        CreateMenu();
                    }
                }
                    break;
                case EventType.MouseDrag:
                {
                    // 拖拽
                    if (Event.current.button == 2 && Position.Contains(Event.current.mousePosition))
                    {
                        Context.DragOffset += Event.current.delta;
                        Event.current.Use();
                    }
                }
                    break;
                case EventType.ScrollWheel:
                {
                    // 缩放
                    if (Position.Contains(Event.current.mousePosition))
                    {
                        // 当 f = Event.current.delta.y 为正数或零时，返回值为 1，当 f 为负数时，返回值为 -1。
                        Context.ZoomFactor -= Mathf.Sign(Event.current.delta.y) / 20f;
                        Context.ZoomFactor = Mathf.Clamp(Context.ZoomFactor, 0.2f, 1f);
                        Event.current.Use();
                    }
                }
                    break;
                case EventType.DragUpdated:
                {
                    if (DragAndDrop.objectReferences[0] is ComboConfig comboConfig)
                    {
                        DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                        Event.current.Use();
                    }
                }
                    break;
                case EventType.DragExited:
                {
                    if (DragAndDrop.objectReferences[0] is ComboConfig comboConfig)
                    {
                        CreatePlayNode(comboConfig);
                        Event.current.Use();
                    }
                }
                    break;
            }
        }

        private void CreateMenu()
        {
            GenericMenu genericMenu = new GenericMenu();
            genericMenu.AddItem(new GUIContent("Create Node"), false, () => { CreatePlayNode(); });
            genericMenu.ShowAsContext();
        }

        private ComboGraphPlayNode CreatePlayNode()
        {
            var nodePosition = new Rect(0, 0, NodeWidth, NodeHeight)
            {
                center = GetMousePosition(_mousePosition)
            };
            var node = Context.CreatePlayNode();
            if (!node)
            {
                return null;
            }

            node.rect = nodePosition;
            Context.SelectNode(node);
            return node;
        }

        private ComboGraphPlayNode CreatePlayNode(ComboConfig comboConfig)
        {
            var nodePosition = new Rect(0, 0, NodeWidth, NodeHeight)
            {
                center = GetMousePosition(_mousePosition)
            };
            var node = Context.CreatePlayNode(comboConfig);
            if (!node)
            {
                return null;
            }

            node.rect = nodePosition;
            Context.SelectNode(node);
            return node;
        }

        private void DrawGrid(Rect rect, float gridSpace, Color color)
        {
            if (rect.width < gridSpace)
            {
                return;
            }

            if (gridSpace == 0)
            {
                return;
            }

            gridSpace *= Context.ZoomFactor;
            DrawHorizontal(rect, gridSpace, color);
            DrawHorizontal(rect, -gridSpace, color, 1);
            DrawVertical(rect, gridSpace, color);
            DrawVertical(rect, -gridSpace, color, 1);
        }

        private void DrawVertical(Rect rect, float gradSpace, Color color, int startIndex = 0)
        {
            Vector2 center = rect.center + Context.DragOffset;
            Vector2 start;
            Vector2 end;

            int i = startIndex;

            if (center.x > rect.position.x + rect.width && gradSpace < 0)
            {
                i = Mathf.CeilToInt((center.x - (rect.position.x + rect.width)) / Mathf.Abs(gradSpace));
            }

            if (center.x < rect.position.x && gradSpace > 0)
            {
                i = Mathf.CeilToInt((rect.position.x - center.x) / Mathf.Abs(gradSpace));
            }

            GUIExtension.Begin();
            do
            {
                start = new Vector2(center.x + gradSpace * i, rect.center.y - rect.height / 2);
                end = new Vector2(center.x + gradSpace * i, rect.center.y + rect.height / 2);
                if (rect.Contains((start + end) / 2))
                {
                    GUIExtension.DrawLine(start, end, 5, color);
                    i++;
                }
            } while (rect.Contains((start + end) / 2));

            GUIExtension.End();
        }

        private void DrawHorizontal(Rect rect, float gradSpace, Color color, int startIndex = 0)
        {
            Vector2 center = rect.center + Context.DragOffset;
            Vector2 start;
            Vector2 end;

            int i = startIndex;

            if (center.y > rect.position.y + rect.height && gradSpace < 0)
            {
                i = Mathf.CeilToInt((center.y - (rect.position.y + rect.height)) / Mathf.Abs(gradSpace));
            }

            if (center.y < rect.position.y && gradSpace > 0)
            {
                i = Mathf.CeilToInt((rect.position.y - center.x) / Mathf.Abs(gradSpace));
            }

            GUIExtension.Begin();
            do
            {
                start = new Vector2(rect.center.x - rect.width / 2, center.y + gradSpace * i);
                end = new Vector2(rect.center.x + rect.width / 2, center.y + gradSpace * i);
                if (rect.Contains((start + end) / 2))
                {
                    GUIExtension.DrawLine(start, end, 5, color);
                    i++;
                }
            } while (rect.Contains((start + end) / 2));

            GUIExtension.End();
        }

        private bool IsMouseOverAnyNode()
        {
            foreach (var item in Context.TotalNodes)
            {
                if (GetTransformRect(item.rect).MouseOn())
                {
                    return true;
                }
            }

            return false;
        }
    }
}