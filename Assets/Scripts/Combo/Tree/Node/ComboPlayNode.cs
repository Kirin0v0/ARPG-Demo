// using System;
// using System.Collections.Generic;
// using System.Linq;
// using Action;
// using Character;
// using Combo.Blackboard;
// using Combo.Tree;
// using Common;
// using Damage;
// using Framework.Common.BehaviourTree.Node;
// using Framework.Common.BehaviourTree.Node.Composite;
// using Framework.Common.Blackboard;
// using Framework.Common.Debug;
// using Player;
// using Sirenix.OdinInspector;
// using UnityEngine;
//
// namespace Combo.Tree.Node
// {
//     [NodeMenuItem("Combo/Play")]
//     public class ComboPlayNode : CompositeNode, IBlackboardProvide
//     {
//         [NonSerialized] public DamageManager DamageManager;
//         [NonSerialized] public AlgorithmManager AlgorithmManager;
//         
//         [InfoBox("配置连招前请设置黑板stage参数，值如下:\nstart-0  anticipation-1\njudgment-2  recovery-3\nend-4  stop-5")]
//         [Header("连招配置")]
//         [SerializeField]
//         private string comboName;
//
//         public string ComboName => comboName;
//
//         [SerializeField] private ComboConfig comboConfig;
//         public ComboConfig ComboConfig => comboConfig;
//
//         [Header("连招转向配置")] [SerializeField]
//         private ComboTurnStrategy comboTurnStrategy = ComboTurnStrategy.TurnToSinglePlayerInput;
//
//         [HideIf("comboTurnStrategy", ComboTurnStrategy.None)] [SerializeField]
//         private ComboTurnStage comboTurnStage = ComboTurnStage.Anticipation;
//
//         private Quaternion _comboTurnInitialDirection;
//         private Quaternion _comboTurnTargetDirection;
//
//         private float _tickTime;
//         private bool _startTick;
//         private PlayerCharacterObject _playerCharacterObject;
//         private IComboPlay _comboPlayer;
//         private List<ComboTip> _comboTips;
//
//         protected override string DefaultDescription =>
//             "连招播放节点，子节点可包含连招切换节点和连招退出节点，执行以下逻辑：\n" +
//             "1.每帧执行切换节点和退出节点的条件逻辑，如果满足条件则返回失败;\n" +
//             "2.最终执行连招每帧逻辑，并根据连招阶段设置黑板stage参数";
//
//         protected override void OnStart(object payload)
//         {
//             var comboTreeParameters = (ComboTreeParameters)payload;
//             _startTick = true;
//             _tickTime = 0f;
//             _playerCharacterObject = comboTreeParameters.playerCharacter;
//
//             // 初始化连招播放器
//             _comboPlayer = new ComboPlayer(
//                 comboConfig,
//                 comboTreeParameters.playerCharacter,
//                 comboTreeParameters.audioManager,
//                 DamageManager,
//                 AlgorithmManager,
//                 comboTreeParameters.audioParent,
//                 comboTreeParameters.effectParent,
//                 comboTreeParameters.colliderDetectionParent,
//                 blackboard is ComboBlackboard comboBlackboard ? comboBlackboard.AllowCollide : null
//             );
//             _comboPlayer.OnStageChanged += HandleComboStageChanged;
//
//             // 记录当前连招的提示
//             if (comboTreeParameters.executor.tree is ComboTree comboTree)
//             {
//                 _comboTips = comboTree.GetNextComboTips(this);
//             }
//         }
//
//         protected override NodeState OnTick(float deltaTime, object payload)
//         {
//             var comboTreeParameters = (ComboTreeParameters)payload;
//             // 展示连招提示
//             comboTreeParameters.onComboPlay?.Invoke(comboConfig, _comboTips);
//
//             // 在执行连招前执行连招切换和连招退出子节点，满足则直接返回成功
//             foreach (var child in children)
//             {
//                 if ((child is ComboSwitchNode switchNode && (switchNode.SwitchTime & ComboSwitchTime.Before) != 0) ||
//                     (child is ComboExitNode exitNode && (exitNode.ExitTime & ComboExitTime.Before) != 0))
//                 {
//                     if (child.Tick(deltaTime, payload) == NodeState.Success)
//                     {
//                         if (debug)
//                         {
//                             DebugUtil.LogGreen($"连招播放节点({comboName})满足切换或退出条件({child.name})");
//                         }
//
//                         _comboPlayer.Stop();
//                         blackboard.SetIntParameter("stage", 5);
//                         return NodeState.Success;
//                     }
//                 }
//             }
//
//             // 如果不满足切换和退出条件才会执行接下来的逻辑
//             var nodeState = NodeState.Running;
//             if (_startTick)
//             {
//                 _startTick = false;
//                 _comboPlayer.Start();
//             }
//             else
//             {
//                 _tickTime += deltaTime;
//                 // 执行连招每帧逻辑
//                 _comboPlayer.Tick(deltaTime);
//             }
//
//             switch (_comboPlayer.Stage)
//             {
//                 case ComboStage.Idle:
//                     blackboard.SetIntParameter("stage", -1);
//                     break;
//                 case ComboStage.Start:
//                     blackboard.SetIntParameter("stage", 0);
//                     break;
//                 case ComboStage.Anticipation:
//                     // 处理指定阶段的连招转向
//                     if (comboTurnStage == ComboTurnStage.Anticipation)
//                     {
//                         HandlePlayerCharacterTurn(comboTreeParameters,
//                             _tickTime - deltaTime < comboConfig.actionClip.process.anticipationTime,
//                             deltaTime);
//                     }
//
//                     blackboard.SetIntParameter("stage", 1);
//                     break;
//                 case ComboStage.Judgment:
//                     // 处理指定阶段的连招转向
//                     if (comboTurnStage == ComboTurnStage.Judgment)
//                     {
//                         HandlePlayerCharacterTurn(comboTreeParameters,
//                             _tickTime - deltaTime < comboConfig.actionClip.process.judgmentTime,
//                             deltaTime);
//                     }
//
//                     blackboard.SetIntParameter("stage", 2);
//                     break;
//                 case ComboStage.Recovery:
//                     // 处理指定阶段的连招转向
//                     if (comboTurnStage == ComboTurnStage.Recovery)
//                     {
//                         HandlePlayerCharacterTurn(comboTreeParameters,
//                             _tickTime - deltaTime < comboConfig.actionClip.process.recoveryTime,
//                             deltaTime);
//                     }
//
//                     blackboard.SetIntParameter("stage", 3);
//                     break;
//                 case ComboStage.End:
//                     blackboard.SetIntParameter("stage", 4);
//                     nodeState = NodeState.Success;
//                     break;
//                 case ComboStage.Stop:
//                     blackboard.SetIntParameter("stage", 5);
//                     break;
//             }
//
//             if (debug)
//             {
//                 DebugUtil.LogGreen($"连招播放节点({comboName})当前播放累计时间: {_tickTime}");
//             }
//
//             // 在执行连招后执行连招切换和连招退出子节点，满足则直接返回成功
//             foreach (var child in children)
//             {
//                 if ((child is ComboSwitchNode switchNode && (switchNode.SwitchTime & ComboSwitchTime.After) != 0) ||
//                     (child is ComboExitNode exitNode && (exitNode.ExitTime & ComboExitTime.After) != 0))
//                 {
//                     if (child.Tick(deltaTime, payload) == NodeState.Success)
//                     {
//                         if (debug)
//                         {
//                             DebugUtil.LogGreen($"连招播放节点({comboName})满足切换或退出条件({child.name})");
//                         }
//
//                         _comboPlayer.Stop();
//                         blackboard.SetIntParameter("stage", 5);
//                         return NodeState.Success;
//                     }
//                 }
//             }
//
//             return nodeState;
//         }
//
//         protected override void OnAbort(object payload)
//         {
//             base.OnAbort(payload);
//
//             if (stopWhenAbort)
//             {
//                 _comboPlayer.Stop();
//                 blackboard.SetIntParameter("stage", 5);
//             }
//         }
//
//         protected override void OnStop(object payload)
//         {
//             base.OnStop(payload);
//
//             _comboPlayer.OnStageChanged -= HandleComboStageChanged;
//         }
//
// #if UNITY_EDITOR
//         public override bool AddChildNode(Framework.Common.BehaviourTree.Node.Node child)
//         {
//             if (child is not ComboSwitchNode && child is not ComboExitNode)
//             {
//                 DebugUtil.LogWarning("The child node of ComboPlayNode must be ComboSwitchNode or ComboExitNode");
//                 return false;
//             }
//
//             return base.AddChildNode(child);
//         }
//
//         public override bool RemoveChildNode(Framework.Common.BehaviourTree.Node.Node child)
//         {
//             if (child is not ComboSwitchNode && child is not ComboExitNode)
//             {
//                 DebugUtil.LogWarning("The child node of ComboPlayNode must be ComboSwitchNode or ComboExitNode");
//                 return false;
//             }
//
//             return base.RemoveChildNode(child);
//         }
// #endif
//
//         private void HandleComboStageChanged(ComboStage stage)
//         {
//             switch (stage)
//             {
//                 case ComboStage.Idle:
//                     if (debug)
//                     {
//                         DebugUtil.LogGreen($"连招播放节点({comboName})阶段: Idle");
//                     }
//
//                     break;
//                 case ComboStage.Start:
//                     if (debug)
//                     {
//                         DebugUtil.LogGreen($"连招播放节点({comboName})阶段: Start");
//                     }
//
//                     break;
//                 case ComboStage.Anticipation:
//                     if (debug)
//                     {
//                         DebugUtil.LogGreen($"连招播放节点({comboName})阶段: Anticipation");
//                     }
//
//                     break;
//                 case ComboStage.Judgment:
//                     if (debug)
//                     {
//                         DebugUtil.LogGreen($"连招播放节点({comboName})阶段: Judgment");
//                     }
//
//                     break;
//                 case ComboStage.Recovery:
//                     if (debug)
//                     {
//                         DebugUtil.LogGreen($"连招播放节点({comboName})阶段: Recovery");
//                     }
//
//                     break;
//                 case ComboStage.End:
//                     if (debug)
//                     {
//                         DebugUtil.LogGreen($"连招播放节点({comboName})阶段: End");
//                     }
//
//                     break;
//                 case ComboStage.Stop:
//                     if (debug)
//                     {
//                         DebugUtil.LogGreen($"连招播放节点({comboName})阶段: Stop");
//                     }
//
//                     break;
//             }
//
//             // 在判定阶段采用预输入，判定阶段后停止预输入
//             if (stage == ComboStage.Judgment)
//             {
//                 _playerCharacterObject.Brain.StartInputBuffer();
//             }
//             else if (stage > ComboStage.Judgment)
//             {
//                 _playerCharacterObject.Brain.StopInputBuffer();
//             }
//         }
//
//         private void HandlePlayerCharacterTurn(ComboTreeParameters comboTreeParameters, bool firstTickInStage,
//             float deltaTime)
//         {
//             switch (comboTurnStrategy)
//             {
//                 case ComboTurnStrategy.TurnToSinglePlayerInput:
//                 {
//                     if (firstTickInStage)
//                     {
//                         _comboTurnInitialDirection = comboTreeParameters.playerCharacter.transform.rotation;
//                         // 如果玩家当前没有输入，则使用当前朝向作为目标朝向
//                         if (comboTreeParameters.playerCharacter
//                                 .PlayerParameters.playerInputMovementInFrame.sqrMagnitude == 0f)
//                         {
//                             _comboTurnTargetDirection = comboTreeParameters.playerCharacter.transform.rotation;
//                         }
//                         else
//                         {
//                             _comboTurnTargetDirection = Quaternion.LookRotation(comboTreeParameters
//                                 .playerCharacter
//                                 .PlayerParameters.playerInputMovementInFrame);
//                         }
//                     }
//
//                     var comboTurnInitialTime = comboTurnStage switch
//                     {
//                         ComboTurnStage.Anticipation => comboConfig.actionClip.process.anticipationTime,
//                         ComboTurnStage.Judgment => comboConfig.actionClip.process.judgmentTime,
//                         ComboTurnStage.Recovery => comboConfig.actionClip.process.recoveryTime,
//                     };
//                     var comboTurnEndTime = comboTurnStage switch
//                     {
//                         ComboTurnStage.Anticipation => Mathf.Min(comboConfig.actionClip.process.anticipationTime + 0.1f,
//                             comboConfig.actionClip.process.judgmentTime),
//                         ComboTurnStage.Judgment => Mathf.Min(comboConfig.actionClip.process.judgmentTime + 0.1f,
//                             comboConfig.actionClip.process.recoveryTime),
//                         ComboTurnStage.Recovery => Mathf.Min(comboConfig.actionClip.process.recoveryTime + 0.1f,
//                             comboConfig.actionClip.process.duration),
//                     };
//                     // 将玩家向单次输入方向插值转向
//                     comboTreeParameters.playerCharacter.MovementAbility?.RotateTo(Quaternion.Slerp(
//                         _comboTurnInitialDirection,
//                         _comboTurnTargetDirection,
//                         Mathf.Clamp01((_tickTime - comboTurnInitialTime) / (comboTurnEndTime - comboTurnInitialTime))
//                     ));
//                 }
//                     break;
//                 case ComboTurnStrategy.TurnToContinuousPlayerInput:
//                 {
//                     _comboTurnInitialDirection = comboTreeParameters.playerCharacter.transform.rotation;
//                     // 如果玩家当前没有输入，则使用当前朝向作为目标朝向
//                     if (comboTreeParameters.playerCharacter
//                             .PlayerParameters.playerInputMovementInFrame.sqrMagnitude == 0f)
//                     {
//                         _comboTurnTargetDirection = comboTreeParameters.playerCharacter.transform.rotation;
//                     }
//                     else
//                     {
//                         _comboTurnTargetDirection = Quaternion.LookRotation(comboTreeParameters.playerCharacter
//                             .PlayerParameters.playerInputMovementInFrame);
//                     }
//
//                     // 将玩家持续向输入方向插值转向
//                     comboTreeParameters.playerCharacter.MovementAbility?.RotateTo(Quaternion.Slerp(
//                         _comboTurnInitialDirection,
//                         _comboTurnTargetDirection,
//                         5 * deltaTime
//                     ));
//                 }
//                     break;
//             }
//         }
//
//         public Framework.Common.Blackboard.Blackboard Blackboard => blackboard;
//     }
// }