using System.Collections.Generic;
using System.Linq;
using Framework.Common.Debug;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Framework.Common.Blackboard.Editor
{
    [CustomPropertyDrawer(typeof(BlackboardVariable))]
    public class BlackboardVariableDrawer : PropertyDrawer
    {
        private const float WidgetMinWidth = 60f;
        private const float WidgetSpace = 15f;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            EditorGUI.BeginChangeCheck();

            Blackboard blackboard = null;
            if (property.serializedObject.targetObject is Blackboard)
            {
                blackboard = property.serializedObject.targetObject as Blackboard;
            }
            else if (property.serializedObject.targetObject is IBlackboardProvide)
            {
                blackboard = (property.serializedObject.targetObject as IBlackboardProvide)?.Blackboard;
            }

            if (blackboard)
            {
                var widgetWidth = Mathf.Max((position.width - WidgetSpace) / 2, WidgetMinWidth);

                // 获取属性
                var matchProperty = property.FindPropertyRelative("match");
                var keyProperty = property.FindPropertyRelative("key");
                var typeProperty = property.FindPropertyRelative("type");
                var intValueProperty = property.FindPropertyRelative("intValue");
                var floatValueProperty = property.FindPropertyRelative("floatValue");
                var boolValueProperty = property.FindPropertyRelative("boolValue");
                var stringValueProperty = property.FindPropertyRelative("stringValue");

                // 设置条件参数，如果没找到就认为参数消失，不再寻找，除非是第一次创建时会寻找默认参数
                var parameterIndex =
                    blackboard.parameters.FindIndex(variable => variable.key == keyProperty.stringValue);
                if (parameterIndex == -1)
                {
                    if (!matchProperty.boolValue)
                    {
                        if (blackboard.parameters.Count != 0)
                        {
                            keyProperty.stringValue = blackboard.parameters[0].key;
                            typeProperty.enumValueIndex = (int)blackboard.parameters[0].type;
                            parameterIndex = 0;
                            matchProperty.boolValue = true;
                        }
                    }
                }
                else
                {
                    matchProperty.boolValue = true;
                }

                // 绘制参数选择UI
                var parameterRect = new Rect(position.x, position.y, widgetWidth, position.height);
                if (parameterIndex < 0)
                {
                    parameterIndex++;
                    var parameterArray = new string[1 + blackboard.parameters.Count];
                    parameterArray[0] = "";
                    for (int i = 0; i < blackboard.parameters.Count; i++)
                    {
                        parameterArray[i + 1] = blackboard.parameters[i].key;
                    }

                    parameterIndex = EditorGUI.Popup(parameterRect, parameterIndex, parameterArray) - 1;
                }
                else
                {
                    parameterIndex = EditorGUI.Popup(parameterRect, parameterIndex,
                        blackboard.parameters.Select(variable => variable.key).ToArray());
                }

                // 如果设置了有效参数就同步数据
                if (parameterIndex >= 0)
                {
                    keyProperty.stringValue = blackboard.parameters[parameterIndex].key;
                    typeProperty.enumValueIndex = (int)blackboard.parameters[parameterIndex].type;
                }

                position.x += widgetWidth + WidgetSpace;
                // 如果没有有效参数就提示用户，否则继续绘制后续的值UI
                if (parameterIndex < 0)
                {
                    EditorGUI.LabelField(position, "Parameter does not exist");
                }
                else
                {
                    // 绘制参数值UI
                    var conditionRect = new Rect(position.x, position.y, widgetWidth, position.height);
                    switch (typeProperty.enumValueIndex)
                    {
                        case (int)BlackboardVariableType.Int:
                            intValueProperty.intValue =
                                EditorGUI.DelayedIntField(conditionRect, intValueProperty.intValue);
                            break;
                        case (int)BlackboardVariableType.Float:
                            floatValueProperty.floatValue =
                                EditorGUI.DelayedFloatField(conditionRect, floatValueProperty.floatValue);
                            break;
                        case (int)BlackboardVariableType.Bool:
                            boolValueProperty.boolValue =
                                EditorGUI.Toggle(conditionRect, boolValueProperty.boolValue);
                            break;
                        case (int)BlackboardVariableType.String:
                            stringValueProperty.stringValue =
                                EditorGUI.DelayedTextField(conditionRect, stringValueProperty.stringValue);
                            break;
                    }
                }
            }
            else
            {
                EditorGUI.LabelField(position, "Don't read blackboard");
            }

            if (EditorGUI.EndChangeCheck())
            {
                property.serializedObject.ApplyModifiedProperties();
            }

            EditorGUI.EndProperty();
        }
    }
}