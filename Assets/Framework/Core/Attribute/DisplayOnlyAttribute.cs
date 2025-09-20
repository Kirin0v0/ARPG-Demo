#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace Framework.Core.Attribute
{
    /// <summary>
    /// Inspector仅显示不可修改特性
    /// </summary>
    public class DisplayOnlyAttribute : PropertyAttribute
    {
    }

#if UNITY_EDITOR
    /// <summary>
    /// DisplayOnly对应的实现逻辑
    /// </summary>
    [CustomPropertyDrawer(typeof(DisplayOnlyAttribute))]
    public class DisplayOnlyDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            GUI.enabled = false;
            // EditorGUI.BeginDisabledGroup(true);
            EditorGUI.PropertyField(position, property, label, false);
            // EditorGUI.BeginDisabledGroup(false);
            GUI.enabled = true;
        }
    }
#endif
}