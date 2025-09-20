using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Framework.Common.BehaviourTree.Editor.UI
{
    public class BehaviourTreeInspectorView : VisualElement
    {
        public new class UXmlFactory : UxmlFactory<BehaviourTreeInspectorView, VisualElement.UxmlTraits>
        {
        }

        private UnityEditor.Editor _nodeEditor;

        public BehaviourTreeInspectorView()
        {
        }

        internal void HandleNodeSelected(BehaviourTreeNodeView nodeView)
        {
            ClearNode();

            // 使用节点Editor渲染内容
            var scrollView = new ScrollView();
            _nodeEditor = UnityEditor.Editor.CreateEditor(nodeView.Node);
            var container = new IMGUIContainer(() =>
            {
                // EditorGUILayout.ObjectField(nodeView.Node, typeof(Node.Node));
                if (_nodeEditor != null && _nodeEditor.target)
                {
                    _nodeEditor.OnInspectorGUI();
                }
            });
            scrollView.Add(container);
            Add(scrollView);
        }

        internal void HandleNodeUnselected(BehaviourTreeNodeView nodeView)
        {
            if (_nodeEditor.target == nodeView.Node)
            {
                ClearNode();
            }
        }

        private void ClearNode()
        {
            Clear();
            if (_nodeEditor)
            {
                GameObject.DestroyImmediate(_nodeEditor);
                _nodeEditor = null;
            }
        }
    }
}