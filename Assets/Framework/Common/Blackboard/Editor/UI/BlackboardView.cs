using Framework.Common.Debug;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Framework.Common.Blackboard.Editor.UI
{
    public class BlackboardView : VisualElement
    {
        public new class UXmlFactory : UxmlFactory<BlackboardView, VisualElement.UxmlTraits>
        {
        }

        private UnityEditor.Editor _blackboardEditor;

        public BlackboardView()
        {
        }

        internal void UpdateView(Blackboard blackboard)
        {
            // 清除UI内容
            Clear();
            if (_blackboardEditor)
            {
                GameObject.DestroyImmediate(_blackboardEditor);
                _blackboardEditor = null;
            }

            if (!blackboard)
            {
                return;
            }

            // 使用黑板Editor渲染内容
            var scrollView = new ScrollView();
            _blackboardEditor = UnityEditor.Editor.CreateEditor(blackboard);
            scrollView.Add(_blackboardEditor.CreateInspectorGUI());
            Add(scrollView);
        }
    }
}