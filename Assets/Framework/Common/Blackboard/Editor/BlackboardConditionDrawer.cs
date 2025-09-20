using System.Collections.Generic;
using System.Linq;
using Framework.Common.Debug;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Framework.Common.Blackboard.Editor
{
    [CustomPropertyDrawer(typeof(BlackboardCondition))]
    public class BlackboardConditionDrawer : PropertyDrawer
    {
        private const float WidgetMinWidth = 60f;
        private const float WidgetSpace = 15f;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            EditorGUI.BeginChangeCheck();

            var blackboard = (property.serializedObject.targetObject as IBlackboardProvide)?.Blackboard;

            if (blackboard)
            {
                var widgetWidth = Mathf.Max(position.width / 3, WidgetMinWidth);

                // 获取属性
                var matchVariableProperty = property.FindPropertyRelative("matchVariable");
                var keyProperty = property.FindPropertyRelative("key");
                var typeProperty = property.FindPropertyRelative("type");
                var comparisonProperty = property.FindPropertyRelative("comparison");
                var intConditionProperty = property.FindPropertyRelative("intCondition");
                var floatConditionProperty = property.FindPropertyRelative("floatCondition");
                var boolConditionProperty = property.FindPropertyRelative("boolCondition");
                var stringConditionProperty = property.FindPropertyRelative("stringCondition");

                // 设置条件参数，如果没找到就认为参数消失，不再寻找，除非是第一次创建时会寻找默认参数
                var parameterIndex =
                    blackboard.parameters.FindIndex(variable => variable.key == keyProperty.stringValue);
                if (parameterIndex == -1)
                {
                    if (!matchVariableProperty.boolValue)
                    {
                        if (blackboard.parameters.Count != 0)
                        {
                            keyProperty.stringValue = blackboard.parameters[0].key;
                            typeProperty.enumValueIndex = (int)blackboard.parameters[0].type;
                            parameterIndex = 0;
                            matchVariableProperty.boolValue = true;
                        }
                    }
                }
                else
                {
                    matchVariableProperty.boolValue = true;
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
                // 如果没有有效参数就提示用户，否则继续绘制后续的比较和条件值UI
                if (parameterIndex < 0)
                {
                    EditorGUI.LabelField(position, "Parameter does not exist");
                }
                else
                {
                    widgetWidth = Mathf.Max(position.width / 3 - WidgetSpace, WidgetMinWidth);
                    var variable = blackboard.parameters[parameterIndex];
                    // 判断参数类型，如果是布尔值就额外处理UI，其他类型需要先绘制比较规则再绘制条件UI
                    if (variable.type == BlackboardVariableType.Bool)
                    {
                        // 绘制条件值UI
                        position.x += widgetWidth + WidgetSpace;
                        var conditionRect = new Rect(position.x, position.y, widgetWidth, position.height);
                        boolConditionProperty.boolValue =
                            EditorGUI.Toggle(conditionRect, boolConditionProperty.boolValue);
                    }
                    else
                    {
                        // 绘制比较选择UI
                        var comparisonRect = new Rect(position.x, position.y, widgetWidth, position.height);
                        if (variable.type == BlackboardVariableType.String)
                        {
                            if (comparisonProperty.enumValueIndex != (int)BlackboardConditionComparison.Equal &&
                                comparisonProperty.enumValueIndex != (int)BlackboardConditionComparison.NotEqual)
                            {
                                comparisonProperty.enumValueIndex = (int)BlackboardConditionComparison.Equal;
                            }

                            var comparisonArray = new[]
                            {
                                "Equal",
                                "NotEqual",
                            };
                            comparisonProperty.enumValueIndex = EditorGUI.Popup(comparisonRect,
                                comparisonProperty.enumValueIndex - (int)BlackboardConditionComparison.Equal,
                                comparisonArray) + (int)BlackboardConditionComparison.Equal;
                        }
                        else
                        {
                            var comparisonArray = new[]
                            {
                                "Greater",
                                "Less",
                                "Equal",
                                "NotEqual",
                            };
                            comparisonProperty.enumValueIndex = EditorGUI.Popup(comparisonRect,
                                comparisonProperty.enumValueIndex, comparisonArray);
                        }

                        // 绘制条件值UI
                        position.x += widgetWidth + WidgetSpace;
                        var conditionRect = new Rect(position.x, position.y, widgetWidth, position.height);
                        switch (variable.type)
                        {
                            case BlackboardVariableType.Int:
                                intConditionProperty.intValue =
                                    EditorGUI.DelayedIntField(conditionRect, intConditionProperty.intValue);
                                break;
                            case BlackboardVariableType.Float:
                                floatConditionProperty.floatValue =
                                    EditorGUI.DelayedFloatField(conditionRect, floatConditionProperty.floatValue);
                                break;
                            case BlackboardVariableType.String:
                                stringConditionProperty.stringValue =
                                    EditorGUI.DelayedTextField(conditionRect, stringConditionProperty.stringValue);
                                break;
                        }
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