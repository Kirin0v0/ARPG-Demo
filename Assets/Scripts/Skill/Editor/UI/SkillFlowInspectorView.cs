using Skill.Unit;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Skill.Editor.UI
{
    public class SkillFlowInspectorView : VisualElement
    {
        public new class UXmlFactory : UxmlFactory<SkillFlowInspectorView, VisualElement.UxmlTraits>
        {
        }

        private UnityEditor.Editor _nodeEditor;

        public SkillFlowInspectorView()
        {
        }

        internal void HandleNodeSelected(SkillFlowNode flowNode)
        {
            ClearNode();

            // 使用节点Editor渲染内容
            var scrollView = new ScrollView();
            _nodeEditor = UnityEditor.Editor.CreateEditor(flowNode);
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

        internal void HandleNodeUnselected(SkillFlowNode flowNode)
        {
            if (_nodeEditor.target == flowNode)
            {
                ClearNode();
            }
        }
        
        private void ClearNode() {
            // 清除UI内容
            Clear();
            if (_nodeEditor)
            {
                GameObject.DestroyImmediate(_nodeEditor);
                _nodeEditor = null;
            }
        }
    }
}