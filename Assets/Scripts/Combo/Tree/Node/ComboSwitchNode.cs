// using System;
// using System.Collections.Generic;
// using System.Linq;
// using Combo.Blackboard;
// using Framework.Common.BehaviourTree.Node;
// using Framework.Common.BehaviourTree.Node.Action;
// using Framework.Common.Blackboard;
// using Framework.Common.Debug;
// using UnityEngine;
// using UnityEngine.Serialization;
//
// namespace Combo.Tree.Node
// {
//     [Flags]
//     public enum ComboSwitchTime
//     {
//         None = 0,
//         Before = 1 << 0,
//         After = 1 << 1,
//     }
//
//     [NodeMenuItem("Combo/Switch")]
//     public class ComboSwitchNode : ActionNode, IBlackboardProvide
//     {
//         [SerializeField] private List<BlackboardConditionOperator> andConditions;
//         public List<BlackboardConditionOperator> AndConditions => andConditions;
//
//         [SerializeField] private List<BlackboardVariable> variables;
//
//         [SerializeField] private string comboName;
//         public string ComboName => comboName;
//
//         [SerializeField] private ComboSwitchTime switchTime = ComboSwitchTime.Before;
//         public ComboSwitchTime SwitchTime => switchTime;
//
//         protected override string DefaultDescription => "连招切换节点，条件为与逻辑，用于从某一连招节点切换到其他连招节点";
//
//         protected override NodeState OnTick(float deltaTime, object payload)
//         {
//             if (Satisfy(false))
//             {
//                 foreach (var variable in variables)
//                 {
//                     blackboard.SetParameter(variable);
//                 }
//
//                 return NodeState.Success;
//             }
//
//             return NodeState.Failure;
//         }
//
//         public bool Satisfy(bool ignoreUnnecessaryConditions = false)
//         {
//             return andConditions.Count == 0 ||
//                    andConditions.All(conditionOperator =>
//                    {
//                        var conditions = conditionOperator.conditions.Where(condition =>
//                        {
//                            if (blackboard is not ComboBlackboard comboBlackboard) return true;
//                            // 如果没有找到对应提示，则默认加入满足条件范围内
//                            var index = comboBlackboard.Tips.FindIndex(tipVariable => tipVariable.key == condition.key);
//                            if (index == -1) return true;
//                            var tipVariable = comboBlackboard.Tips[index];
//                            // 如果存在提示配置且不开启忽略非必要条件，则在变量为非必要变量时不加入满足条件范围内
//                            return !ignoreUnnecessaryConditions || !tipVariable.unnecessaryCondition;
//                        }).ToList();
//                        return conditions.Satisfy(conditionOperator.type, blackboard);
//                    });
//         }
//
//         public Framework.Common.Blackboard.Blackboard Blackboard => blackboard;
//     }
// }