// using System;
// using System.Collections.Generic;
// using System.Linq;
// using Character;
// using Combo.Tree.Node;
// using Framework.Common.BehaviourTree;
// using Framework.Common.BehaviourTree.Node;
// using Player;
// using UnityEngine;
// using UnityEngine.Serialization;
//
// namespace Combo.Tree
// {
//     public class ComboTreeExecutor : BehaviourTreeExecutor
//     {
//         [SerializeField] private ComboTreeParameters parameters;
//
//         public bool AllowExecute()
//         {
//             if (tree is not ComboTree)
//             {
//                 return false;
//             }
//
//             if (tree.rootNode is ComboRootNode comboRootNode)
//             {
//                 return comboRootNode.AllowEnter();
//             }
//
//             return false;
//         }
//
//         public override NodeState Tick(float deltaTime)
//         {
//             return tree.Tick(deltaTime, parameters);
//         }
//     }
// }