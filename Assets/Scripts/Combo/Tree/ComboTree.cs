// using System;
// using System.Collections.Generic;
// using System.Linq;
// using Combo.Blackboard;
// using Combo.Tree.Node;
// using Framework.Common.BehaviourTree;
// using Framework.Common.BehaviourTree.Node;
// using Framework.Common.Blackboard;
// using UnityEditor;
// using UnityEngine;
//
// namespace Combo.Tree
// {
//     [CreateAssetMenu(menuName = "Combo/Tree")]
//     public class ComboTree : BehaviourTree
//     {
//         private bool _comboConfigInit = false;
//         private readonly Dictionary<string, ComboConfig> _comboConfigs = new();
//
// #if UNITY_EDITOR
//         public override void CreateBlackboard()
//         {
//             var blackboard = ScriptableObject.CreateInstance<ComboBlackboard>();
//             blackboard.name = "Combo Blackboard";
//             if (!Application.isPlaying) // 文件附加到另一资源文件需要在非运行状态才能执行
//             {
//                 AssetDatabase.AddObjectToAsset(blackboard, this);
//             }
//
//             this.blackboard = blackboard;
//             Dfs(rootNode, node => node.blackboard = blackboard);
//             EditorUtility.SetDirty(this);
//             AssetDatabase.SaveAssets();
//         }
//
//         public override Framework.Common.BehaviourTree.Node.Node CreateRootNode()
//         {
//             return CreateNode(typeof(ComboRootNode));
//         }
// #endif
//
//         public override NodeState Tick(float deltaTime, object payload)
//         {
//             if (blackboard is ComboBlackboard comboBlackboard)
//             {
//                 // 这里每帧计算连招树的全局共享碰撞间隔  
//                 comboBlackboard.CalculateGlobalSharedCollideInterval(deltaTime);
//             }
//
//             return base.Tick(deltaTime, payload);
//         }
//
//         public List<ComboTip> GetEntryTips(bool matchSatisfyConditions)
//         {
//             if (rootNode is ComboRootNode comboRootNode && blackboard is ComboBlackboard comboBlackboard)
//             {
//                 if (comboRootNode.EntryNode)
//                 {
//                     var entryTips = new List<ComboTip>();
//                     List<ComboSwitchNode> comboSwitchNodes;
//                     // 判断是否仅获取满足条件的连招
//                     if (matchSatisfyConditions)
//                     {
//                        // 是则获取满足条件的连招
//                         comboSwitchNodes = comboRootNode.EntryNode.ComboSwitchNodes.Where(switchNode => switchNode.Satisfy(true)).ToList();
//                     }
//                     else
//                     { 
//                         // 否则获取所有连招
//                         comboSwitchNodes = comboRootNode.EntryNode.ComboSwitchNodes;
//                     }
//                     comboSwitchNodes.ForEach(switchNode =>
//                     {
//                         var comboTip = GetComboTipFromSwitchNode(comboBlackboard, switchNode);
//                         if (comboTip.HasValue)
//                         {
//                             entryTips.Add(comboTip.Value);
//                         }
//                     });
//
//                     return entryTips;
//                 }
//
//                 return new List<ComboTip>();
//             }
//
//             return new List<ComboTip>();
//         }
//
//         public List<ComboTip> GetNextComboTips(ComboPlayNode comboPlayNode)
//         {
//             var comboTips = new List<ComboTip>();
//             // 遍历连招播放节点的子节点
//             foreach (var child in comboPlayNode.children)
//             {
//                 // 如果为连招切换节点就继续匹配条件
//                 if (child is ComboSwitchNode switchNode && blackboard is ComboBlackboard comboBlackboard)
//                 {
//                     var comboTip = GetComboTipFromSwitchNode(comboBlackboard, switchNode);
//                     if (comboTip.HasValue)
//                     {
//                         // 这里额外新增逻辑：如果子节点切换连招与当前连招同名，就忽略该节点（用于同一招式的不同动作，例如空中攻击收招等）
//                         if (comboPlayNode.ComboConfig != null &&
//                             comboTip.Value.ComboConfig != null &&
//                             comboTip.Value.ComboConfig.name == comboPlayNode.ComboConfig.name)
//                         {
//                             continue;
//                         }
//
//                         comboTips.Add(comboTip.Value);
//                     }
//                 }
//             }
//
//             return comboTips;
//         }
//
//         private ComboTip? GetComboTipFromSwitchNode(ComboBlackboard comboBlackboard, ComboSwitchNode switchNode)
//         {
//             if (!_comboConfigInit)
//             {
//                 _comboConfigs.Clear();
//                 Dfs(rootNode, node =>
//                 {
//                     if (node is ComboPlayNode comboPlayNode && !String.IsNullOrEmpty(comboPlayNode.ComboName) &&
//                         comboPlayNode.ComboConfig)
//                     {
//                         _comboConfigs.Add(comboPlayNode.ComboName, comboPlayNode.ComboConfig);
//                     }
//                 });
//                 _comboConfigInit = true;
//             }
//             
//             // 如果没获取到连招配置就不继续匹配连招条件
//             if (!_comboConfigs.TryGetValue(switchNode.ComboName, out var comboConfig))
//             {
//                 return null;
//             }
//
//             // 匹配对应的连招条件
//             var operatorTips = new List<ComboConditionOperatorTip>();
//             switchNode.AndConditions.ForEach(conditionOperator =>
//             {
//                 var tips = new List<ComboInputTip>();
//                 conditionOperator.conditions.ForEach(condition =>
//                 {
//                     var tipIndex = comboBlackboard.Tips.FindIndex(inputTip =>
//                     {
//                         if (inputTip.key == condition.key &&
//                             inputTip.type == condition.type)
//                         {
//                             switch (inputTip.type)
//                             {
//                                 case BlackboardVariableType.Int:
//                                     return inputTip.intValue == condition.intCondition;
//                                 case BlackboardVariableType.Float:
//                                     return Mathf.Approximately(inputTip.floatValue,
//                                         condition.floatCondition);
//                                 case BlackboardVariableType.Bool:
//                                     return inputTip.boolValue == condition.boolCondition;
//                                 case BlackboardVariableType.String:
//                                     return inputTip.stringValue == condition.stringCondition;
//                             }
//                         }
//
//                         return false;
//                     });
//                     if (tipIndex != -1)
//                     {
//                         tips.AddRange(comboBlackboard.Tips[tipIndex].tips);
//                     }
//                 });
//                 if (tips.Count != 0)
//                 {
//                     operatorTips.Add(new ComboConditionOperatorTip
//                     {
//                         OperatorType = conditionOperator.type,
//                         Tips = tips,
//                     });
//                 }
//             });
//
//             if (operatorTips.Count != 0)
//             {
//                 return new ComboTip
//                 {
//                     ComboConfig = comboConfig,
//                     OperatorTips = operatorTips,
//                 };
//             }
//
//             return null;
//         }
//     }
// }