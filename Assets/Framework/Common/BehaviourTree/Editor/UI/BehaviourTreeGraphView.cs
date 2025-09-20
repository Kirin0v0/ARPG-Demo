using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Framework.Common.BehaviourTree.Node;
using Framework.Common.BehaviourTree.Node.Action;
using Framework.Common.BehaviourTree.Node.Composite;
using Framework.Common.BehaviourTree.Node.Decorator;
using Framework.Common.Debug;
using Framework.Common.Util;
using Framework.DataStructure;
using Unity.Plastic.Newtonsoft.Json;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace Framework.Common.BehaviourTree.Editor.UI
{
    [Serializable]
    public class NodeCopyData
    {
        public List<NodeCopyItemData> nodes;
    }

    [Serializable]
    public class NodeCopyItemData
    {
        public string type;
        public SerializableVector3 position;
        public string comment;
        public bool stopWhenAbort;
    }

    public class BehaviourTreeGraphView : GraphView
    {
        public new class UXmlFactory : UxmlFactory<BehaviourTreeGraphView, GraphView.UxmlTraits>
        {
        }

        public System.Action<BehaviourTreeNodeView> OnNodeSelected;
        public System.Action<BehaviourTreeNodeView> OnNodeUnselected;
        public Vector2 MousePosition;

        private BehaviourTree _tree;

        private readonly List<Edge> _reconnectEdges = new();
        private readonly List<Edge> _disconnectEdges = new();

        public BehaviourTreeGraphView()
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
                AssetDatabase.LoadAssetAtPath<StyleSheet>(
                    "Assets/Framework/Common/BehaviourTree/Editor/BehaviourTreeEditorWindow.uss");
            styleSheets.Add(styleSheet);

            // 监听撤销事件
            Undo.undoRedoPerformed += HandleRedoPerformed;

            #region 监听剪切/复制/黏贴事件

            serializeGraphElements += (elements =>
            {
                var nodes = new List<NodeCopyItemData>();
                foreach (var graphElement in elements)
                {
                    if (graphElement is BehaviourTreeNodeView nodeView)
                    {
                        var nodeCopyData = new NodeCopyItemData
                        {
                            type = nodeView.Node.GetType().Name,
                            position = new SerializableVector3(nodeView.Node.position),
                            comment = nodeView.Node.comment,
                            stopWhenAbort = nodeView.Node.StopWhenAbort,
                        };
                        nodes.Add(nodeCopyData);
                    }
                }

                var data = new NodeCopyData()
                {
                    nodes = nodes,
                };
                var nodeJson = JsonConvert.SerializeObject(data);
                return nodeJson;
            });
            canPasteSerializedData += data => true;
            unserializeAndPaste += (operationName, data) =>
            {
                var nodeCopyData = JsonConvert.DeserializeObject<NodeCopyData>(data);
                // 创建节点
                foreach (var node in nodeCopyData.nodes)
                {
                    Type nodeType = null;
                    var typeCollection = TypeCache.GetTypesDerivedFrom<Node.Node>();
                    foreach (var type in typeCollection)
                    {
                        if (type.Name.Equals(node.type))
                        {
                            nodeType = type;
                        }
                    }

                    if (nodeType == null)
                    {
                        continue;
                    }

                    var newNode = _tree?.CreateNode(nodeType);
                    if (newNode)
                    {
                        var nodeView = CreateNodeView(newNode, false);
                        var newPosition = nodeView.GetPosition();
                        newPosition.x = node.position.x + 40;
                        newPosition.y = node.position.y + 40;
                        nodeView.SetPosition(newPosition);
                        nodeView.Node.comment = node.comment;
                        nodeView.Node.stopWhenAbort = node.stopWhenAbort;
                        EditorUtility.SetDirty(nodeView.Node);
                    }
                }
            };

            #endregion

            schedule.Execute(HandleEdgeReconnectedOrDisconnected).Every(50);
        }

        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            if (_tree == null)
            {
                return;
            }

            var actionNodeTypes = TypeCache.GetTypesDerivedFrom<ActionNode>();
            AddMenuItem(actionNodeTypes);
            var decoratorNodeTypes = TypeCache.GetTypesDerivedFrom<DecoratorNode>();
            AddMenuItem(decoratorNodeTypes);
            var compositeNodeTypes = TypeCache.GetTypesDerivedFrom<CompositeNode>();
            AddMenuItem(compositeNodeTypes);

            void AddMenuItem(TypeCache.TypeCollection types)
            {
                foreach (var type in types)
                {
                    if (type.IsAbstract)
                    {
                        continue;
                    }
                    
                    if (type.IsDefined(typeof(NodeMenuItem), false))
                    {
                        var attributes = type.GetCustomAttributes(typeof(NodeMenuItem), false);
                        if (attributes.Length != 0)
                        {
                            var attribute = attributes[0] as NodeMenuItem;
                            evt.menu.AppendAction(attribute.ItemName, _ =>
                            {
                                var node = _tree.CreateNode(type);
                                CreateNodeView(node, true);
                            });
                        }
                        else
                        {
                            evt.menu.AppendAction($"{type.BaseType.Name}/{type.Name}", _ =>
                            {
                                var node = _tree.CreateNode(type);
                                CreateNodeView(node, true);
                            });
                        }
                    }
                    else
                    {
                        evt.menu.AppendAction($"{type.BaseType.Name}/{type.Name}", _ =>
                        {
                            var node = _tree.CreateNode(type);
                            CreateNodeView(node, true);
                        });
                    }
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
        /// <param name="tree"></param>
        internal void UpdateView(BehaviourTree tree)
        {
            _tree = tree;
            graphViewChanged -= OnGraphViewChanged;
            DeleteElements(graphElements);
            if (!tree)
            {
                return;
            }

            graphViewChanged += OnGraphViewChanged;
            focusable = true;

            // 如果没有根节点就创建根节点
            if (!tree.rootNode)
            {
                tree.rootNode = tree.CreateRootNode();
            }

            // 没有黑板就创建黑板
            if (!tree.blackboard)
            {
                tree.CreateBlackboard();
            }

            // 创建节点UI
            tree.nodes.ForEach(node => CreateNodeView(node, false));

            // 创建图连接
            tree.nodes.ForEach(node =>
            {
                var parentNodeView = FindNodeView(node);
                tree.GetChildNodes(node).ForEach(childNode =>
                {
                    var childNodeView = FindNodeView(childNode);
                    var edge = parentNodeView.Output.ConnectTo(childNodeView.Input);
                    AddElement(edge);
                });
            });

            // 节点重排序
            SortAllNodeViews();
        }

        internal void UpdateNodeStates()
        {
            nodes.ForEach(node =>
            {
                if (node is BehaviourTreeNodeView treeNodeView)
                {
                    treeNodeView.UpdateState();
                }
            });
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
                    if (graphElement is BehaviourTreeNodeView nodeView)
                    {
                        _tree?.DeleteNode(nodeView.Node);
                    }

                    // 图边删除时删除节点的子节点
                    if (graphElement is Edge edge)
                    {
                        var parentNodeView = edge.output.node as BehaviourTreeNodeView;
                        var childNodeView = edge.input.node as BehaviourTreeNodeView;
                        if (!_tree || !_tree.RemoveChildNode(parentNodeView!.Node, childNodeView!.Node))
                        {
                            _reconnectEdges.Add(edge);
                        }
                    }

                    // 图节点或边删除时所有子节点重新排序
                    SortAllNodeViews();
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
                    var parentNodeView = edge.output.node as BehaviourTreeNodeView;
                    var childNodeView = edge.input.node as BehaviourTreeNodeView;
                    if (!_tree || !_tree.AddChildNode(parentNodeView!.Node, childNodeView!.Node))
                    {
                        _disconnectEdges.Add(edge);
                    }
                }

                // 图边创建时所有子节点重新排序
                SortAllNodeViews();
            }

            void HandleElementsMoved()
            {
                if (graphViewChange.movedElements == null)
                {
                    return;
                }

                // 图节点移动时对所有子节点重新排序
                SortAllNodeViews();
            }
        }

        private void HandleRedoPerformed()
        {
            if (!_tree)
            {
                return;
            }

            UpdateView(_tree);
            AssetDatabase.SaveAssets();
        }

        private BehaviourTreeNodeView CreateNodeView(Node.Node node, bool isNewNode)
        {
            var nodeView = new BehaviourTreeNodeView(node)
            {
                OnNodeSelected = OnNodeSelected,
                OnNodeUnselected = OnNodeUnselected
            };
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

        private BehaviourTreeNodeView FindNodeView(Node.Node node)
        {
            return GetNodeByGuid(node.guid) as BehaviourTreeNodeView;
        }

        private void SortAllNodeViews()
        {
            foreach (var node in nodes)
            {
                if (node is BehaviourTreeNodeView nodeView)
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