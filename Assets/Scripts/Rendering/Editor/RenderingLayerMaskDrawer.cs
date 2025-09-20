using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Rendering.Editor
{
    [CustomPropertyDrawer(typeof(RenderingLayerMask))]
    public class RenderingLayerMaskDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var currentRenderPipeline = GraphicsSettings.currentRenderPipeline;
            if (currentRenderPipeline)
            {
                var layerMask = property.FindPropertyRelative("layerMask");
                EditorGUI.showMixedValue = layerMask.hasMultipleDifferentValues;
                var renderingLayerMask = layerMask.intValue;
                var displayedOptions = currentRenderPipeline.prefixedRenderingLayerMaskNames;
                EditorGUI.BeginChangeCheck();
                var controlRect = EditorGUILayout.GetControlRect();
                EditorGUI.BeginProperty(controlRect, label, layerMask);
                var num = EditorGUI.MaskField(controlRect, label, renderingLayerMask,
                    displayedOptions);
                EditorGUI.EndProperty();
                if (EditorGUI.EndChangeCheck())
                {
                    layerMask.uintValue = (uint)num;
                    property.serializedObject.ApplyModifiedProperties();
                }

                EditorGUI.showMixedValue = false;
            }
        }
    }
}