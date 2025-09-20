using System;
using System.Collections.Generic;
using UnityEngine;

namespace Framework.Common.Blackboard
{
    public enum BlackboardConditionComparison
    {
        Greater,
        Less,
        Equal,
        NotEqual,
    }

    [Serializable]
    public class BlackboardCondition
    {
        public string key;
        public BlackboardVariableType type;
        public BlackboardConditionComparison comparison;
        public int intCondition;
        public float floatCondition;
        public bool boolCondition;
        public string stringCondition;
        public bool matchVariable; // 该字段仅在BlackboardConditionDrawer使用

        public virtual bool Satisfy(Blackboard blackboard)
        {
            // 没有匹配到的参数不影响条件判断
            if (!blackboard.ContainsParameter(key))
            {
                return true;
            }

            switch (type)
            {
                case BlackboardVariableType.Int:
                    var intValue = blackboard.GetIntParameter(key);
                    return comparison switch
                    {
                        BlackboardConditionComparison.Greater => intValue > intCondition,
                        BlackboardConditionComparison.Less => intValue < intCondition,
                        BlackboardConditionComparison.Equal => intValue == intCondition,
                        BlackboardConditionComparison.NotEqual => intValue != intCondition,
                        _ => false,
                    };
                case BlackboardVariableType.Float:
                    var floatValue = blackboard.GetFloatParameter(key);
                    return comparison switch
                    {
                        BlackboardConditionComparison.Greater => floatValue > floatCondition,
                        BlackboardConditionComparison.Less => floatValue < floatCondition,
                        BlackboardConditionComparison.Equal => Mathf.Approximately(floatValue, floatCondition),
                        BlackboardConditionComparison.NotEqual => !Mathf.Approximately(floatValue, floatCondition),
                        _ => false,
                    };
                case BlackboardVariableType.Bool:
                    var booleanValue = blackboard.GetBoolParameter(key);
                    return booleanValue == boolCondition;
                case BlackboardVariableType.String:
                    var stringValue = blackboard.GetStringParameter(key);
                    return comparison switch
                    {
                        BlackboardConditionComparison.Equal => stringValue == stringCondition,
                        BlackboardConditionComparison.NotEqual => stringValue != stringCondition,
                        _ => false,
                    };
            }

            return false;
        }
    }
}