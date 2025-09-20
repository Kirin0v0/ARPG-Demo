using System.Collections.Generic;
using Combo.Graph.Unit;
using UnityEditor;
using UnityEngine;

namespace Combo.Graph.Editor
{
    public class ComboGraphContext
    {
        public event System.Action<ComboGraph> OnGraphChanged;
        public event System.Action<ComboGraphNode> OnGraphNodeSelected;
        public event System.Action<ComboGraphTransition> OnGraphTransitionSelected;
        public event System.Action OnGraphNoneSelected;

        private ComboGraph _comboGraph;

        public ComboGraph ComboGraph
        {
            set
            {
                if (_comboGraph == value)
                {
                    return;
                }

                Reset();
                _comboGraph = value;
                OnGraphChanged?.Invoke(_comboGraph);
            }
            get => _comboGraph;
        }

        public List<ComboGraphNode> TotalNodes => !ComboGraph ? new() : ComboGraph.nodes;

        public List<ComboGraphTransition> TotalTransitions => !ComboGraph ? new() : ComboGraph.transitions;

        private readonly List<ComboGraphNode> _selectedNodes = new();
        private ComboGraphTransition _selectedTransition;

        public bool InTransitionPreview;
        public ComboGraphNode PreviewFromNode { private set; get; }
        public ComboGraphNode PreviewToNode { private set; get; }

        public float ZoomFactor = 0.3f;
        public Vector2 DragOffset = Vector2.zero;

        private readonly ComboGraphEditorWindow _editorWindow;

        public ComboGraphContext(ComboGraphEditorWindow editorWindow)
        {
            _editorWindow = editorWindow;
        }

        public void StartPreviewTransition(ComboGraphNode from)
        {
            InTransitionPreview = true;
            PreviewFromNode = from;
        }

        public void FinishPreviewTransition(ComboGraphNode to)
        {
            PreviewToNode = to;
        }

        public void ClearPreviewTransition()
        {
            InTransitionPreview = false;
            PreviewFromNode = null;
            PreviewToNode = null;
        }

        public void SelectNode(ComboGraphNode node)
        {
            _selectedTransition = null;
            _selectedNodes.Clear();
            _selectedNodes.Add(node);
            OnGraphNodeSelected?.Invoke(node);
            Repaint();
        }

        public void SelectNodes(List<ComboGraphNode> nodes)
        {
            _selectedTransition = null;
            _selectedNodes.Clear();
            _selectedNodes.AddRange(nodes);
            Repaint();
        }

        public void UnselectNodes()
        {
            _selectedNodes.Clear();
            OnGraphNoneSelected?.Invoke();
            Repaint();
        }

        public void UnselectTransition()
        {
            _selectedTransition = null;
            OnGraphNoneSelected?.Invoke();
            Repaint();
        }

        public void MoveSelectedNodes(Vector2 deltaPosition)
        {
            _selectedNodes.ForEach(node =>
            {
                Undo.RecordObject(node, "Combo Graph Node(Move)");
                node.rect.position += deltaPosition;
                EditorUtility.SetDirty(node);
            });
            Repaint();
        }

        public void SelectTransition(ComboGraphTransition transition)
        {
            _selectedNodes.Clear();
            _selectedTransition = transition;
            OnGraphTransitionSelected?.Invoke(transition);
            Repaint();
        }

        public ComboGraphNode CreateEntryNode()
        {
            return !ComboGraph ? null : ComboGraph.CreateEntryNode();
        }

        public ComboGraphNode CreateExitNode()
        {
            return !ComboGraph ? null : ComboGraph.CreateExitNode();
        }

        public ComboGraphPlayNode CreatePlayNode()
        {
            return !ComboGraph ? null : ComboGraph.CreatePlayNode();
        }

        public ComboGraphPlayNode CreatePlayNode(ComboConfig comboConfig)
        {
            return !ComboGraph ? null : ComboGraph.CreatePlayNode(comboConfig);
        }

        public bool DeleteNode(ComboGraphNode node)
        {
            if (!ComboGraph)
            {
                return false;
            }

            var result = ComboGraph.DeleteNode(node);
            if (result)
            {
                _selectedNodes.Remove(node);
                Repaint();
            }

            return result;
        }

        public void DeleteSelectedNodes()
        {
            if (!ComboGraph)
            {
                return;
            }

            var deleteNodes = new List<ComboGraphNode>();
            _selectedNodes.ForEach(node =>
            {
                if (ComboGraph.DeleteNode(node))
                {
                    deleteNodes.Add(node);
                }
            });

            deleteNodes.ForEach(node => _selectedNodes.Remove(node));
            Repaint();
        }

        public ComboGraphTransition CreateTransition(ComboGraphNode from, ComboGraphNode to)
        {
            if (!ComboGraph)
            {
                return null;
            }

            return ComboGraph.CreateTransition(from, to);
        }

        public bool DeleteTransition(ComboGraphTransition transition)
        {
            if (!ComboGraph)
            {
                return false;
            }

            var result = ComboGraph.DeleteTransition(transition);
            if (result && _selectedTransition == transition)
            {
                _selectedTransition = null;
                Repaint();
            }

            return result;
        }

        public void DeleteSelectedTransition()
        {
            if (_selectedTransition)
            {
                DeleteTransition(_selectedTransition);
            }
        }

        public enum ComboGraphNodeType
        {
            Combo,
            Entry,
            Exit,
        }

        public ComboGraphNodeType GetNodeType(ComboGraphNode node)
        {
            if (IsEntryNode(node))
            {
                return ComboGraphNodeType.Entry;
            }

            if (IsExitNode(node))
            {
                return ComboGraphNodeType.Exit;
            }

            return ComboGraphNodeType.Combo;
        }

        public bool IsNodeSelected(ComboGraphNode node)
        {
            return _selectedNodes.Contains(node);
        }

        public bool IsEntryNode(ComboGraphNode node)
        {
            return ComboGraph && ComboGraph.entry == node;
        }

        public bool IsExitNode(ComboGraphNode node)
        {
            return ComboGraph && ComboGraph.exit == node;
        }

        public bool IsNodePlayedOrPlaying(ComboGraphNode node)
        {
            return node.Playing;
        }

        public bool IsTransitionSelected(ComboGraphTransition transition)
        {
            return _selectedTransition == transition;
        }

        public void Reset()
        {
            ZoomFactor = 0.3f;
            DragOffset = Vector2.zero;
            ClearPreviewTransition();
            _selectedNodes.Clear();
            Repaint();
        }

        public void Repaint()
        {
            _editorWindow.Repaint();
        }
    }
}