using Combo.Graph.Unit;
using UnityEngine;
using UnityEngine.UIElements;

namespace Combo.Graph.Editor.UI
{
    public class ComboGraphInspectorView : VisualElement
    {
        public new class UXmlFactory : UxmlFactory<ComboGraphInspectorView, VisualElement.UxmlTraits>
        {
        }

        private UnityEditor.Editor _editor;

        public ComboGraphInspectorView()
        {
        }

        internal void HandleNodeSelected(ComboGraphNode node)
        {
            // 清除UI内容
            Clear();
            if (_editor)
            {
                GameObject.DestroyImmediate(_editor);
                _editor = null;
            }
        
            // 使用节点Editor渲染内容
            var scrollView = new ScrollView();
            _editor = UnityEditor.Editor.CreateEditor(node);
            var container = new IMGUIContainer(() =>
            {
                // EditorGUILayout.ObjectField(nodeView.Node, typeof(Node.Node));
                if (_editor != null && _editor.target)
                {
                    _editor.OnInspectorGUI();
                }
            });
            scrollView.Add(container);
            Add(scrollView);
        }

        internal void HandleTransitionSelected(ComboGraphTransition transition)
        {
            // 清除UI内容
            Clear();
            if (_editor)
            {
                GameObject.DestroyImmediate(_editor);
                _editor = null;
            }
        
            // 使用过渡Editor渲染内容
            var scrollView = new ScrollView();
            _editor = UnityEditor.Editor.CreateEditor(transition);
            var container = new IMGUIContainer(() =>
            {
                // EditorGUILayout.ObjectField(nodeView.Node, typeof(Node.Node));
                if (_editor != null && _editor.target)
                {
                    _editor.OnInspectorGUI();
                }
            });
            scrollView.Add(container);
            Add(scrollView);
        }

        internal void HandleNoneSelected()
        {
            // 清除UI内容
            Clear();
            if (_editor)
            {
                GameObject.DestroyImmediate(_editor);
                _editor = null;
            }
        }
    }
}