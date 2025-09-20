using System;
using System.Collections.Generic;
using System.Linq;
using Framework.Common.LitJson;

namespace Framework.Common.Blackboard
{
    public enum BlackboardConditionOperatorType
    {
        And,
        Or
    }

    [Serializable]
    public class BlackboardConditionOperator
    {
        public BlackboardConditionOperatorType type;
        public List<BlackboardCondition> conditions;

        public bool Satisfy(Blackboard blackboard)
        {
            return conditions.Satisfy(type, blackboard);
        }
    }

    public static class BlackboardConditionOperatorExtension
    {
        public static bool Satisfy(this List<BlackboardCondition> conditions, BlackboardConditionOperatorType type, Blackboard blackboard)
        {
            if (conditions.Count == 0)
            {
                return true;
            }

            return type switch
            {
                BlackboardConditionOperatorType.And => conditions.All(condition => condition.Satisfy(blackboard)),
                BlackboardConditionOperatorType.Or => conditions.Any(condition => condition.Satisfy(blackboard)),
                _ => true
            };
        }
    } 
}