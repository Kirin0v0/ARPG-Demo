using System;
using System.Collections.Generic;
using System.Linq;
using Combo.Blackboard;
using Framework.Common.Blackboard;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Combo.Graph.Unit
{
    /// <summary>
    /// 连招图过渡前置阶段，注意这里值需要和ComboStage的值匹配
    /// </summary>
    [Flags]
    public enum ComboGraphTransitionPreconditionsStage
    {
        Start = 1 << 1,
        Anticipation = 1 << 2,
        Judgment = 1 << 3,
        Recovery = 1 << 4,
        End = 1 << 5,
    }

    public class ComboGraphTransition : ScriptableObject, IBlackboardProvide
    {
#if UNITY_EDITOR
        [HideInInspector] public string guid;
#endif
        [NonSerialized] public ComboGraph Graph;
        
        [ReadOnly] public ComboGraphNode from;
        [ReadOnly] public ComboGraphNode to;
        [HideInInspector] public ComboBlackboard blackboard;

        [ShowIf("@from is ComboGraphPlayNode")] [EnumToggleButtons] [InfoBox("当前节点为连招播放节点时，需要配置跳转的前置阶段（即哪些阶段允许跳转）")]
        public ComboGraphTransitionPreconditionsStage preconditionsStage;

        public List<BlackboardCondition> conditions;

        [NonSerialized] public bool Passed;

        public bool Transit(bool ignoreUnnecessaryConditions = false)
        {
            // 先检查是否满足播放节点的前置阶段
            if (from is ComboGraphPlayNode playNode)
            {
                if (!ignoreUnnecessaryConditions && ((int)playNode.Stage & (int)preconditionsStage) == 0)
                {
                    return false;
                }
            }

            // 再检查黑板条件
            return conditions.Count == 0 || conditions.Where(condition =>
            {
                if (blackboard is not ComboBlackboard comboBlackboard) return true;
                // 如果没有找到对应提示，则默认加入满足条件范围内
                var index = Array.FindIndex(comboBlackboard.Tips, tipVariable => tipVariable.key == condition.key);
                if (index == -1) return true;
                var tipVariable = comboBlackboard.Tips[index];
                // 如果存在提示配置且不开启忽略非必要条件，则在变量为非必要变量时不加入满足条件范围内
                return !ignoreUnnecessaryConditions || !tipVariable.unnecessaryCondition;
            }).All(condition => condition.Satisfy(blackboard));
        }

        public Framework.Common.Blackboard.Blackboard Blackboard => blackboard;
    }
}