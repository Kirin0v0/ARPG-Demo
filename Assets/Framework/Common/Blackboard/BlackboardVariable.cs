using System;
using UnityEngine;

namespace Framework.Common.Blackboard
{
    public enum BlackboardVariableType
    {
        Int,
        Float,
        Bool,
        String,
    }

    [Serializable]
    public class BlackboardVariable
    {
        public string key;
        public BlackboardVariableType type;
        public int intValue;
        public float floatValue;
        public bool boolValue;
        public string stringValue;
        public bool match; // 该字段仅在BlackboardVariableDrawer使用

        protected bool Equals(BlackboardVariable other)
        {
            if (key != other.key || type != other.type)
            {
                return false;
            }

            return type switch
            {
                BlackboardVariableType.Int => intValue == other.intValue,
                BlackboardVariableType.Float => Mathf.Approximately(floatValue, other.floatValue),
                BlackboardVariableType.Bool => boolValue == other.boolValue,
                BlackboardVariableType.String => stringValue.Equals(other.stringValue),
                _ => true,
            };
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj is not BlackboardVariable || this is not BlackboardVariable)
            {
                return false;
            }

            return Equals((BlackboardVariable)obj);
        }

        public override int GetHashCode()
        {
            return type switch
            {
                BlackboardVariableType.Int => HashCode.Combine(key, (int)type, intValue),
                BlackboardVariableType.Float => HashCode.Combine(key, (int)type, floatValue),
                BlackboardVariableType.Bool => HashCode.Combine(key, (int)type, boolValue),
                BlackboardVariableType.String => HashCode.Combine(key, (int)type, stringValue),
                _ => HashCode.Combine(key, (int)type),
            };
        }
    }
}