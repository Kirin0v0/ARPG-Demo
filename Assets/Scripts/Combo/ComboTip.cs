using System.Collections.Generic;
using Combo.Blackboard;
using Framework.Common.Blackboard;

namespace Combo
{
    public struct ComboTip
    {
        public ComboConfig ComboConfig;
        public List<ComboConditionOperatorTip> OperatorTips;
    }

    public struct ComboConditionOperatorTip
    {
        public BlackboardConditionOperatorType OperatorType;
        public List<ComboInputTip> Tips;
    }
}