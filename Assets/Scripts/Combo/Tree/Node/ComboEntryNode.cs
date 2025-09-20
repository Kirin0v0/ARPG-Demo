// using System.Collections.Generic;
// using System.Linq;
// using Action;
// using Combo.Blackboard;
// using Framework.Common.BehaviourTree.Node;
// using Framework.Common.BehaviourTree.Node.Action;
// using Framework.Common.BehaviourTree.Node.Composite;
// using Framework.Common.Blackboard;
// using Inputs;
// using Sirenix.OdinInspector;
// using Sirenix.Utilities;
// using UnityEngine;
// using UnityEngine.Serialization;
//
// namespace Combo.Tree.Node
// {
//     [NodeMenuItem("Combo/Entry")]
//     public class ComboEntryNode : CompositeNode, IBlackboardProvide
//     {
//         [ReadOnly, SerializeField] private List<ComboSwitchNode> comboSwitchNodes = new();
//         [ReadOnly, SerializeField] private List<Framework.Common.BehaviourTree.Node.Node> otherNodes = new();
//
//         private int _lastTickIndex;
//
//         protected override string DefaultDescription =>
//             "连招进入节点。优先按照或逻辑线性执行内部的连招切换节点，如果全部失败则会直接退出连招树。在某个切换节点成功后，才会按照与逻辑线性执行其他节点";
//
//         public List<ComboSwitchNode> ComboSwitchNodes
//         {
//             get
//             {
//                 ArrangeSwitchAndOtherNodes();
//                 return comboSwitchNodes;
//             }
//         }
//
//         public bool AllowEnter()
//         {
//             // 运行时重新整理子节点
//             ArrangeSwitchAndOtherNodes();
//             return comboSwitchNodes.Count != 0 && comboSwitchNodes.Any(switchNode => switchNode.Satisfy(false));
//         }
//
//         protected override void OnStart(object payload)
//         {
//             base.OnStart(payload);
//             // 运行时重新整理子节点
//             ArrangeSwitchAndOtherNodes();
//         }
//
//         protected override NodeState OnTick(float deltaTime, object payload)
//         {
//             var index = _lastTickIndex < 0 ? 0 : _lastTickIndex;
//             if (index >= children.Count)
//             {
//                 return NodeState.Success;
//             }
//
//             // 线性执行子节点
//             while (index < children.Count)
//             {
//                 if (index < comboSwitchNodes.Count)
//                 {
//                     // 按照或逻辑优先一帧内线性执行连招切换节点
//                     var child = comboSwitchNodes[index];
//                     var nodeState = child.Tick(deltaTime, payload);
//                     // 如果子节点处于运行状态就不接着执行后续子节点
//                     if (nodeState == NodeState.Running)
//                     {
//                         _lastTickIndex = index;
//                         return NodeState.Running;
//                     }
//
//                     switch (nodeState)
//                     {
//                         case NodeState.Failure:
//                             index++;
//                             if (index >= comboSwitchNodes.Count)
//                             {
//                                 return NodeState.Failure;
//                             }
//
//                             break;
//                         case NodeState.Success:
//                             index = comboSwitchNodes.Count;
//                             break;
//                     }
//                 }
//                 else
//                 {
//                     // 按照与逻辑一帧内线性执行其他节点
//                     var child = otherNodes[index - comboSwitchNodes.Count];
//                     var nodeState = child.Tick(deltaTime, payload);
//                     // 如果子节点处于运行状态就不接着执行后续子节点
//                     if (nodeState == NodeState.Running)
//                     {
//                         _lastTickIndex = index;
//                         return NodeState.Running;
//                     }
//
//                     switch (nodeState)
//                     {
//                         case NodeState.Failure:
//                             return NodeState.Failure;
//                         case NodeState.Success:
//                             index++;
//                             break;
//                     }
//                 }
//             }
//
//             return NodeState.Success;
//         }
//         
// #if UNITY_EDITOR
//         public override bool AddChildNode(Framework.Common.BehaviourTree.Node.Node child)
//         {
//             var result = base.AddChildNode(child);
//             // 编辑时整理子节点
//             ArrangeSwitchAndOtherNodes();
//             return result;
//         }
//
//         public override bool RemoveChildNode(Framework.Common.BehaviourTree.Node.Node child)
//         {
//             var result = base.RemoveChildNode(child);
//             // 编辑时整理子节点
//             ArrangeSwitchAndOtherNodes();
//             return result;
//         }
// #endif
//
//         private void ArrangeSwitchAndOtherNodes()
//         {
//             comboSwitchNodes.Clear();
//             comboSwitchNodes.AddRange(children.Where(child => child is ComboSwitchNode)
//                 .Convert(child => child as ComboSwitchNode).ToList());
//             otherNodes.Clear();
//             otherNodes.AddRange(children.Where(child => child is not ComboSwitchNode).ToList());
//         }
//
//         public Framework.Common.Blackboard.Blackboard Blackboard => blackboard;
//     }
// }