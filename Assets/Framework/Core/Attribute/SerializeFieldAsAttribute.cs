using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Framework.Core.Attribute
{
    public class SerializeFieldAsAttribute : PropertyAttribute
    {
        public readonly string Alias;

        public SerializeFieldAsAttribute(string alias)
        {
            this.Alias = alias;
        }

#if UNITY_EDITOR
        [CustomPropertyDrawer(typeof(SerializeFieldAsAttribute))]
        public class SerializeFieldAsAttributePropertyDrawer : PropertyDrawer
        {
            public override void OnGUI(Rect position, SerializedProperty property, GUIContent alias)
            {
                var propertyAttribute = this.attribute as SerializeFieldAsAttribute;
                alias.text = propertyAttribute.Alias;
                EditorGUI.PropertyField(position, property, alias);
            }
        }
#endif
    }
}