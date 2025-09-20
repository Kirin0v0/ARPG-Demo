using System;
using System.Collections.Generic;
using System.Linq;
using Framework.Common.BehaviourTree.Node;
using Skill.Editor.UI.Node;
using Skill.Unit;
using Skill.Unit.Feature;
using Skill.Unit.TimelineClip;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace Skill.Editor.UI
{
    public class SkillFlowGraphView : GraphView
    {
        public new class UXmlFactory : UxmlFactory<SkillFlowGraphView, GraphView.UxmlTraits>
        {
        }

        public System.Action<SkillFlowNode> OnNodeSelected;
        public System.Action<SkillFlowNode> OnNodeUnselected;
        public Vector2 MousePosition;

        private SkillFlow _flow;

        private readonly List<Edge> _reconnectEdges = new();
        private readonly List<Edge> _disconnectEdges = new();

        public SkillFlowGraphView()
        {
            var gridBackground = new GridBackground();
            gridBackground.name = "GridBackground";
            Insert(0, gridBackground);

            this.AddManipulator(new ContentZoomer());
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());

            // 注册上下文菜单事件
            RegisterCallback<ContextualMenuPopulateEvent>(_ =>
                MousePosition = Event.current.mousePosition
            );

            var styleSheet =
                AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/Scripts/Skill/Editor/SkillFlowEditorWindow.uss");
            styleSheets.Add(styleSheet);

            // 监听撤销事件
            Undo.undoRedoPerformed += HandleRedoPerformed;

            schedule.Execute(HandleEdgeReconnectedOrDisconnected).Every(50);
        }

        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            if (_flow == null)
            {
                return;
            }

            AddMenuItem(typeof(SkillFlowTimelineNode));
            AddMenuItem(typeof(SkillFlowTimelineNodeNode));
            AddDerivedFromTypeItems(TypeCache.GetTypesDerivedFrom<SkillFlowTimelineClipNode>());
            AddDerivedFromTypeItems(TypeCache.GetTypesDerivedFrom<SkillFlowFeatureNode>());

            void AddDerivedFromTypeItems(TypeCache.TypeCollection types)
            {
                foreach (var type in types)
                {
                    AddMenuItem(type);
                }
            }

            void AddMenuItem(Type type)
            {
                if (type.IsAbstract)
                {
                    return;
                }

                if (type.IsDefined(typeof(NodeMenuItem), false))
                {
                    var attributes = type.GetCustomAttributes(typeof(NodeMenuItem), false);
                    if (attributes.Length != 0)
                    {
                        var attribute = attributes[0] as NodeMenuItem;
                        evt.menu.AppendAction(attribute.ItemName, _ =>
                        {
                            var node = _flow.CreateNode(type);
                            CreateNodeView(node, true);
                        });
                    }
                    else
                    {
                        evt.menu.AppendAction($"{type.BaseType.Name}/{type.Name}", _ =>
                        {
                            var node = _flow.CreateNode(type);
                            CreateNodeView(node, true);
                        });
                    }
                }
                else
                {
                    evt.menu.AppendAction($"{type.BaseType.Name}/{type.Name}", _ =>
                    {
                        var node = _flow.CreateNode(type);
                        CreateNodeView(node, true);
                    });
                }
            }
        }

        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            // 这里是获取在某个端口连接时的满足条件的端口（满足条件是端口输入对输出、且端口对应的节点不同）
            return ports.ToList()
                .Where(endPort => startPort.direction != endPort.direction && startPort.node != endPort.node).ToList();
        }

        /// <summary>
        /// 提供给编辑器的更新UI函数
        /// </summary>
        /// <param name="flow"></param>
        internal void UpdateView(SkillFlow flow)
        {
            _flow = flow;
            graphViewChanged -= OnGraphViewChanged;
            DeleteElements(graphElements);
            if (!flow)
            {
                return;
            }

            graphViewChanged += OnGraphViewChanged;
            focusable = true;

            // 如果没有根节点就创建根节点
            if (!flow.rootNode)
            {
                flow.rootNode = flow.CreateRootNode();
            }

            // 创建节点UI
            flow.nodes.ForEach(node => CreateNodeView(node, false));

            // 创建图连接
            flow.nodes.ForEach(node =>
            {
                var parentNodeView = FindNodeView(node);
                // 先遍历输出端口，再将端口关联创建连接
                node.GetOutputs().ForEach(output =>
                {
                    node.GetChildNodes(output.key).ForEach(childNode =>
                    {
                        var childNodeView = FindNodeView(childNode);
                        var outputPort = parentNodeView.Outputs.Find(port => (string)port.userData == output.key);
                        if (outputPort != null)
                        {
                            var edge = outputPort.ConnectTo(childNodeView.Input);
                            AddElement(edge);
                        }
                    });
                });
            });

            // 节点重排序
            SortRootNodeViews();
        }

        /// <summary>
        /// 图内容变化节点函数，这里处理图数据的改变
        /// </summary>
        /// <param name="graphViewChange"></param>
        /// <returns></returns>
        private GraphViewChange OnGraphViewChanged(GraphViewChange graphViewChange)
        {
            HandleElementsRemoved();
            HandleEdgesCreated();
            HandleElementsMoved();

            return graphViewChange;

            void HandleElementsRemoved()
            {
                if (graphViewChange.elementsToRemove == null)
                {
                    return;
                }

                foreach (var graphElement in graphViewChange.elementsToRemove)
                {
                    // 图节点删除时同步删除行为树数据的节点
                    if (graphElement is SkillFlowNodeView nodeView)
                    {
                        _flow?.DeleteNode(nodeView.Node);
                    }

                    // 图边删除时删除节点的子节点
                    if (graphElement is Edge edge)
                    {
                        var parentNodeView = edge.output.node as SkillFlowNodeView;
                        var childNodeView = edge.input.node as SkillFlowNodeView;
                        if (!_flow || !_flow.RemoveChildNode(parentNodeView!.Node, (string)edge.output.userData,
                                childNodeView!.Node))
                        {
                            _reconnectEdges.Add(edge);
                        }
                    }

                    // 图节点或边删除时所有子节点重新排序
                    SortRootNodeViews();
                }
            }

            void HandleEdgesCreated()
            {
                if (graphViewChange.edgesToCreate == null)
                {
                    return;
                }

                // 图边创建时新增节点的子节点
                foreach (var edge in graphViewChange.edgesToCreate)
                {
                    var parentNodeView = edge.output.node as SkillFlowNodeView;
                    var childNodeView = edge.input.node as SkillFlowNodeView;
                    if (!_flow || !_flow.AddChildNode(parentNodeView!.Node, (string)edge.output.userData,
                            childNodeView!.Node))
                    {
                        _disconnectEdges.Add(edge);
                    }
                }

                // 图边创建时所有子节点重新排序
                SortRootNodeViews();
            }

            void HandleElementsMoved()
            {
                if (graphViewChange.movedElements == null)
                {
                    return;
                }

                // 图节点移动时对所有子节点重新排序
                SortRootNodeViews();
            }
        }

        private void HandleRedoPerformed()
        {
            if (!_flow)
            {
                return;
            }

            UpdateView(_flow);
            AssetDatabase.SaveAssets();
        }

        private SkillFlowNodeView CreateNodeView(SkillFlowNode node, bool isNewNode)
        {
            var nodeView = node switch
            {
                SkillFlowRootNode rootNode => new SkillFlowRootNodeView(rootNode),
                SkillFlowTimelineNode timelineNode => new SkillFlowTimelineNodeView(timelineNode),
                SkillFlowTimelineClipNode timelineClipNode => new SkillFlowTimelineClipNodeView(timelineClipNode),
                SkillFlowTimelineNodeNode timelineNodeNode => new SkillFlowTimelineNodeNodeView(timelineNodeNode),
                _ => new SkillFlowNodeView(node)
            };
            nodeView.OnNodeSelected = OnNodeSelected;
            nodeView.OnNodeUnselected = OnNodeUnselected;
            AddElement(nodeView);
            if (isNewNode)
            {
                // 设置节点位置为鼠标位置
                var position = contentViewContainer.WorldToLocal(MousePosition);
                var newPosition = nodeView.GetPosition();
                newPosition.x = position.x;
                newPosition.y = position.y;
                nodeView.SetPosition(newPosition);
            }

            return nodeView;
        }

        private SkillFlowNodeView FindNodeView(SkillFlowNode node)
        {
            return GetNodeByGuid(node.guid) as SkillFlowNodeView;
        }

        private void SortRootNodeViews()
        {
            foreach (var node in nodes)
            {
                if (node is SkillFlowRootNodeView nodeView)
                {
                    nodeView.SortChildren();
                }
            }
        }

        private void HandleEdgeReconnectedOrDisconnected()
        {
            _reconnectEdges.ForEach(edge =>
            {
                edge.output?.Connect(edge);
                edge.input?.Connect(edge);
                AddElement(edge);
            });
            _reconnectEdges.Clear();
            _disconnectEdges.ForEach(edge =>
            {
                edge.output?.Disconnect(edge);
                edge.input?.Disconnect(edge);
                RemoveElement(edge);
            });
            _disconnectEdges.Clear();
        }
    }
}