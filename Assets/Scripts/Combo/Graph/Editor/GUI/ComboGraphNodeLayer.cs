using System.Collections.Generic;
using System.Linq;
using Combo.Graph.Unit;
using Framework.Common.Debug;
using Framework.Common.Editor.Extension;
using UnityEditor;
using UnityEngine;

namespace Combo.Graph.Editor.GUI
{
    public class ComboGraphNodeLayer : ComboGraphLayer
    {
        private readonly ComboGraphNodeStyle _comboGraphNodeStyle = new();
        private readonly GUIStyle _selectBoxStyle = new("SelectionRect");
        private readonly GUIStyle _playingBackgroundStyle = new("MeLivePlayBackground");

        private bool _prepareSelecting;
        private bool _mouseMoving;
        private bool Selecting => _prepareSelecting && _mouseMoving;
        private Vector2 _startSelectPosition;
        private Rect _selectBox;
        private Rect _playingBackgroundRect;

        public ComboGraphNodeLayer(ComboGraphContext context) : base(context)
        {
        }

        public override void DrawGUI(Rect rect)
        {
            base.DrawGUI(rect);

            List<ComboGraphNode> nodes = Context.TotalNodes;
            if (Event.current.type == EventType.Repaint)
            {
                _comboGraphNodeStyle.ApplyZoomFactory(Context.ZoomFactor);
                nodes.ForEach(DrawNode);
            }
        }

        public override void ConsumeEvent()
        {
            base.ConsumeEvent();

            switch (Event.current.type)
            {
                case EventType.MouseDown:
                    // 处理节点点击事件，如果有节点响应点击事件就不执行后续逻辑
                    List<ComboGraphNode> nodes = Context.TotalNodes;
                    if (nodes.Any(HandleNodeClicked))
                    {
                        break;
                    }

                    // 此时没有任何节点响应点击事件
                    // 判断是否左键按下执行开始选区
                    if (Event.current.button == 0)
                    {
                        _prepareSelecting = true;
                        _startSelectPosition = Event.current.mousePosition;
                    }

                    break;
                case EventType.MouseMove:
                {
                    _mouseMoving = true;
                }
                    break;
                case EventType.MouseUp:
                    // 判断是否左键抬起执行结束选区
                    if (Event.current.button == 0)
                    {
                        _prepareSelecting = false;
                        _mouseMoving = false;
                    }

                    // 判断是否右键抬起创建菜单
                    if (Event.current.button == 1)
                    {
                        foreach (var item in Context.TotalNodes)
                        {
                            if (GetTransformRect(item.rect).MouseOn())
                            {
                                CreateNodeMenu(item);
                                Event.current.Use();
                                break;
                            }
                        }
                    }

                    break;
                case EventType.KeyDown:
                {
                    // Delete键删除选中节点
                    if (Event.current.keyCode == KeyCode.Delete)
                    {
                        Context.DeleteSelectedNodes();
                    }
                }
                    break;
                case EventType.MouseDrag:
                    // 左键拖拽
                    if (Event.current.button == 0)
                    {
                        _mouseMoving = true;
                        if (!Selecting)
                        {
                            Context.MoveSelectedNodes(Event.current.delta / Context.ZoomFactor);
                        }

                        Event.current.Use();
                    }

                    break;
                case EventType.MouseLeaveWindow:
                    // 离开窗口就结束选区
                    _prepareSelecting = false;
                    _mouseMoving = false;
                    break;
            }

            DrawSelectBox();
        }

        private void CreateNodeMenu(ComboGraphNode node)
        {
            switch (Context.GetNodeType(node))
            {
                case ComboGraphContext.ComboGraphNodeType.Entry:
                {
                    var genericMenu = new GenericMenu();
                    genericMenu.AddItem(new GUIContent("Make Transition"), false,
                        () => { Context.StartPreviewTransition(node); });
                    genericMenu.ShowAsContext();
                }
                    break;
                case ComboGraphContext.ComboGraphNodeType.Combo:
                {
                    var genericMenu = new GenericMenu();
                    genericMenu.AddItem(new GUIContent("Make Transition"), false,
                        () => { Context.StartPreviewTransition(node); });
                    genericMenu.AddItem(new GUIContent("Delete"), false, () => { Context.DeleteNode(node); });
                    genericMenu.ShowAsContext();
                }
                    break;
            }
        }

        private void DrawNode(ComboGraphNode node)
        {
            if (!node)
            {
                return;
            }

            Rect rect = GetTransformRect(node.rect);
            if (!Position.Overlaps(rect))
            {
                return;
            }

            UnityEngine.GUI.Box(rect, node.name, GetNodeStyle(node));

            if (Context.IsNodePlayedOrPlaying(node))
            {
                _playingBackgroundRect.Set(rect.x, rect.y + rect.height * 3 / 4, rect.width, rect.height / 4);
                UnityEngine.GUI.Box(_playingBackgroundRect, string.Empty, _playingBackgroundStyle);
            }
        }

        private GUIStyle GetNodeStyle(ComboGraphNode node)
        {
            var isSelected = Context.IsNodeSelected(node);
            var nodeType = Context.GetNodeType(node);
            switch (nodeType)
            {
                case ComboGraphContext.ComboGraphNodeType.Entry:
                    return _comboGraphNodeStyle.GetStyle(isSelected
                        ? ComboGraphStyle.GreenOn
                        : ComboGraphStyle.Green);
                case ComboGraphContext.ComboGraphNodeType.Exit:
                    return _comboGraphNodeStyle.GetStyle(isSelected
                        ? ComboGraphStyle.RedOn
                        : ComboGraphStyle.Red);
                default:
                    if (node is ComboGraphPlayNode playNode)
                    {
                        if (playNode.Playing)
                        {
                            return _comboGraphNodeStyle.GetStyle(isSelected
                                ? ComboGraphStyle.YellowOn
                                : ComboGraphStyle.Yellow);
                        }

                        if (playNode.Played)
                        {
                            return _comboGraphNodeStyle.GetStyle(isSelected
                                ? ComboGraphStyle.BlueOn
                                : ComboGraphStyle.Blue);
                        }
                    }

                    return _comboGraphNodeStyle.GetStyle(isSelected
                        ? ComboGraphStyle.NormalOn
                        : ComboGraphStyle.Normal);
            }
        }

        private bool HandleNodeClicked(ComboGraphNode node)
        {
            if (GetTransformRect(node.rect).MouseOn() && EventExtension.IsMouseDown(0))
            {
                // 判断当前是否处于预览过渡关系中
                if (Context.InTransitionPreview)
                {
                    // 是则创建过渡关系
                    Context.FinishPreviewTransition(node);
                    var transition = Context.CreateTransition(Context.PreviewFromNode, Context.PreviewToNode);
                    if (transition)
                    {
                        Context.SelectTransition(transition);
                    }

                    Context.ClearPreviewTransition();
                }
                else
                {
                    // 否则选中节点
                    if (!Context.IsNodeSelected(node))
                    {
                        Context.SelectNode(node);
                    }
                }

                Event.current.Use();

                return true;
            }

            return false;
        }

        private void DrawSelectBox()
        {
            if (!Selecting)
            {
                _selectBox = Rect.zero;
                return;
            }

            Vector2 delta = Event.current.mousePosition - _startSelectPosition;
            _selectBox.center = _startSelectPosition + delta / 2;
            _selectBox.width = Mathf.Abs(delta.x);
            _selectBox.height = Mathf.Abs(delta.y);
            UnityEngine.GUI.Button(_selectBox, "", _selectBoxStyle);
            Context.SelectNodes(Context.TotalNodes
                .Where(node => GetTransformRect(node.rect).Overlaps(_selectBox, true)).ToList());
        }
    }
}