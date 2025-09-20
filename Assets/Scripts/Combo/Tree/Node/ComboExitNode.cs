// using System;
// using System.Collections.Generic;
// using System.Linq;
// using Framework.Common.BehaviourTree.Node;
// using Framework.Common.BehaviourTree.Node.Action;
// using Framework.Common.Blackboard;
// using Framework.Common.Debug;
// using Sirenix.OdinInspector;
// using UnityEngine;
//
// namespace Combo.Tree.Node
// {
//     [Flags]
//     public enum ComboExitTime
//     {
//         None = 0,
//         Before = 1 << 0,
//         After = 1 << 1,
//     }
//
//     [NodeMenuItem("Combo/Exit")]
//     public class ComboExitNode : ActionNode, IBlackboardProvide
//     {
//         [SerializeField] private List<BlackboardConditionOperator> orConditions;
//         public List<BlackboardConditionOperator> OrConditions => orConditions;
//         [SerializeField] private List<BlackboardVariable> variables;
//         
//         [SerializeField] private ComboExitTime exitTime = ComboExitTime.Before;
//         public ComboExitTime ExitTime => exitTime;
//
//         protected override string DefaultDescription => "连招退出节点，条件为或逻辑，用于退出连招树";
//
//         protected override NodeState OnTick(float deltaTime, object payload)
//         {
//             if (orConditions.Count == 0 || orConditions.Any(condition => condition.Satisfy(blackboard)))
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
//         public Framework.Common.Blackboard.Blackboard Blackboard => blackboard;
//     }
// }