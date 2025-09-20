// using Framework.Common.BehaviourTree.Node;
// using Framework.Common.Debug;
//
// namespace Combo.Tree.Node
// {
//     public class ComboRootNode : RootNode
//     {
//         protected override string DefaultDescription => "连招树根节点，子节点必须是连招进入节点";
//
//         public ComboEntryNode EntryNode => child as ComboEntryNode;
//
//         public bool AllowEnter()
//         {
//             if (child is ComboEntryNode entryNode)
//             {
//                 return entryNode.AllowEnter();
//             }
//
//             return false;
//         }
//
// #if UNITY_EDITOR
//         public override bool AddChildNode(Framework.Common.BehaviourTree.Node.Node child)
//         {
//             if (child is not ComboEntryNode)
//             {
//                 DebugUtil.LogWarning("The child node of ComboRootNode must be ComboEntryNode");
//                 return false;
//             }
//
//             return base.AddChildNode(child);
//         }
//
//         public override bool RemoveChildNode(Framework.Common.BehaviourTree.Node.Node child)
//         {
//             if (child is not ComboEntryNode)
//             {
//                 DebugUtil.LogWarning("The child node of ComboRootNode must be ComboEntryNode");
//                 return false;
//             }
//
//             return base.RemoveChildNode(child);
//         }
// #endif
//     }
// }