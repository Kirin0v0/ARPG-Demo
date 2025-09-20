using System;
using System.Collections.Generic;
using System.Linq;
using Combo.Blackboard;
using Combo.Graph.Unit;
using Framework.Common.Blackboard;
using Framework.Common.Debug;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;
using VContainer;

namespace Combo.Graph
{
    public enum ComboGraphState
    {
        Play,
        Finish,
    }
    
    [CreateAssetMenu(menuName = "Combo/Graph")]
    public class ComboGraph : ScriptableObject
    {
        [HideInInspector] public ComboGraphNode entry;
        [HideInInspector] public ComboGraphNode exit;

        [ReadOnly] public List<ComboGraphNode> nodes;
        [ReadOnly] public List<ComboGraphTransition> transitions;
        [ReadOnly] public ComboBlackboard blackboard;

        [NonSerialized] private ComboGraphNode _currentNode = null;
        public bool Playing { get; private set; }

#if UNITY_EDITOR
        public void CreateBlackboard()
        {
            var newBlackboard = ScriptableObject.CreateInstance<ComboBlackboard>();
            newBlackboard.name = "Combo Blackboard";
            if (!Application.isPlaying) // 文件附加到另一资源文件需要在非运行状态才能执行
            {
                AssetDatabase.AddObjectToAsset(newBlackboard, this);
            }

            blackboard = newBlackboard;
            nodes.ForEach(node =>
            {
                if (node is ComboGraphPlayNode playNode)
                {
                    playNode.blackboard = newBlackboard;
                }
            });
            transitions.ForEach(transition => transition.blackboard = newBlackboard);
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
        }

        public ComboGraphNode CreateEntryNode()
        {
            if (entry)
            {
                return entry;
            }

            Undo.RecordObject(this, "Combo Graph(Create Entry Node)");
            var node = ScriptableObject.CreateInstance(typeof(ComboGraphEntryNode)) as ComboGraphEntryNode;
            node.guid = GUID.Generate().ToString();
            node.name = "Entry";
            nodes.Add(node);

            if (!Application.isPlaying) // 文件附加到另一资源文件需要在非运行状态才能执行
            {
                AssetDatabase.AddObjectToAsset(node, this);
            }

            Undo.RegisterCreatedObjectUndo(node, "Combo Graph(Create Entry Node)");
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();

            return node;
        }

        public ComboGraphNode CreateExitNode()
        {
            if (exit)
            {
                return exit;
            }

            Undo.RecordObject(this, "Combo Graph(Create Exit Node)");
            var node = ScriptableObject.CreateInstance(typeof(ComboGraphExitNode)) as ComboGraphExitNode;
            node.guid = GUID.Generate().ToString();
            node.name = "Exit";
            nodes.Add(node);

            if (!Application.isPlaying) // 文件附加到另一资源文件需要在非运行状态才能执行
            {
                AssetDatabase.AddObjectToAsset(node, this);
            }

            Undo.RegisterCreatedObjectUndo(node, "Combo Graph(Create Exit Node)");
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();

            return node;
        }

        public ComboGraphPlayNode CreatePlayNode()
        {
            Undo.RecordObject(this, "Combo Graph(Create Play Node)");
            var node = ScriptableObject.CreateInstance(typeof(ComboGraphPlayNode)) as ComboGraphPlayNode;
            node.guid = GUID.Generate().ToString();
            node.nodeName = "Undefined";
            node.blackboard = blackboard;
            node.name = node.nodeName;
            nodes.Add(node);

            if (!Application.isPlaying) // 文件附加到另一资源文件需要在非运行状态才能执行
            {
                AssetDatabase.AddObjectToAsset(node, this);
            }

            Undo.RegisterCreatedObjectUndo(node, "Combo Graph(Create Play Node)");
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();

            return node;
        }

        /// <summary>
        /// 这里创建不使用Undo记录，因为开发时发现如果调用UndoAPI，Unity会自动删除创建的节点文件
        /// </summary>
        /// <param name="comboConfig"></param>
        /// <returns></returns>
        public ComboGraphPlayNode CreatePlayNode(ComboConfig comboConfig)
        {
            var node = ScriptableObject.CreateInstance(typeof(ComboGraphPlayNode)) as ComboGraphPlayNode;
            node.guid = GUID.Generate().ToString();
            node.nodeName = comboConfig.Name;
            node.comboConfig = comboConfig;
            node.blackboard = blackboard;
            node.name = node.nodeName;
            nodes.Add(node);

            if (!Application.isPlaying) // 文件附加到另一资源文件需要在非运行状态才能执行
            {
                AssetDatabase.AddObjectToAsset(node, this);
            }

            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();

            return node;
        }

        public bool DeleteNode(ComboGraphNode node)
        {
            if (entry == node || exit == node)
            {
                DebugUtil.LogWarning("You can't delete entry or exit node");
                return false;
            }

            Undo.RecordObject(this, "Combo Graph(Delete Node)");
            var removedTransitions = transitions.Where(transition => transition.from == node || transition.to == node)
                .ToList();
            removedTransitions.ForEach(transition => { transitions.Remove(transition); });
            removedTransitions.ForEach(Undo.DestroyObjectImmediate);
            Undo.RecordObject(this, "Combo Graph(Delete Node)");
            nodes.Remove(node);
            Undo.DestroyObjectImmediate(node);
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
            return true;
        }

        public ComboGraphTransition CreateTransition(ComboGraphNode from, ComboGraphNode to)
        {
            if (!from || !to || to == entry || from == exit || from == to)
            {
                DebugUtil.LogWarning("You can't create a wrong transition");
                return null;
            }

            Undo.RecordObject(this, "Combo Graph(Create Transition)");
            var transition = ScriptableObject.CreateInstance(typeof(ComboGraphTransition)) as ComboGraphTransition;
            transition.guid = GUID.Generate().ToString();
            transition.name = "Transition";
            transition.from = from;
            transition.to = to;
            transition.blackboard = blackboard;
            transitions.Add(transition);

            if (!Application.isPlaying) // 文件附加到另一资源文件需要在非运行状态才能执行
            {
                AssetDatabase.AddObjectToAsset(transition, this);
            }

            Undo.RegisterCreatedObjectUndo(transition, "Combo Graph(Create Transition)");
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();

            return transition;
        }

        public bool DeleteTransition(ComboGraphTransition transition)
        {
            Undo.RecordObject(this, "Combo Graph(Delete Transition)");
            transitions.Remove(transition);
            Undo.DestroyObjectImmediate(transition);
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
            return true;
        }
#endif
        /// <summary>
        /// 运行时克隆函数，防止运行时篡改原文件
        /// </summary>
        /// <returns></returns>
        public ComboGraph Clone(IObjectResolver objectResolver)
        {
            var comboGraph = Instantiate(this);
            comboGraph.blackboard = Instantiate(blackboard);
            // 先克隆过渡关系
            comboGraph.transitions = transitions.Select(transition =>
            {
                // 这里先替换过渡关系的黑板
                var graphTransition = Instantiate(transition);
                graphTransition.Graph = comboGraph;
                graphTransition.blackboard = comboGraph.blackboard;
                return graphTransition;
            }).ToList();
            // 再克隆节点
            var cloneNodes = new List<ComboGraphNode>();
            nodes.ForEach(node =>
            {
                var cloneNode = Instantiate(node);
                cloneNode.Graph = comboGraph;
                if (cloneNode is ComboGraphPlayNode playNode)
                {
                    playNode.blackboard = comboGraph.blackboard;
                }
                objectResolver.Inject(cloneNode);
                // 查询之前克隆的过渡关系替换节点
                comboGraph.transitions.ForEach(transition =>
                {
                    if (transition.from == node)
                    {
                        transition.from = cloneNode;
                    }

                    if (transition.to == node)
                    {
                        transition.to = cloneNode;
                    }
                });

                // 如果是入口或出口节点，就设置为对应节点
                if (entry == node)
                {
                    comboGraph.entry = cloneNode;
                }

                if (exit == node)
                {
                    comboGraph.exit = cloneNode;
                }

                cloneNodes.Add(cloneNode);
            });
            comboGraph.nodes = cloneNodes;

            return comboGraph;
        }

        /// <summary>
        /// 销毁图函数，用于彻底销毁自身及自身关联的连招资源
        /// </summary>
        public void Destroy()
        {
            // 销毁自身
            GameObject.Destroy(this);
            // 销毁黑板
            GameObject.Destroy(blackboard);
            // 销毁过渡关系
            transitions.ForEach(GameObject.Destroy);
            transitions.Clear();
            // 销毁内部节点
            nodes.ForEach(GameObject.Destroy);
            nodes.Clear();
        }
        
        /// <summary>
        /// 连招图帧函数，运行时调用
        /// </summary>
        /// <param name="deltaTime"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public ComboGraphState Tick(float deltaTime, ComboGraphParameters parameters)
        {
            // 如果未播放就将当前节点设置为入口节点
            if (!Playing)
            {
                Playing = true;
                _currentNode = entry;
            }

            var previousNode = _currentNode;
            // 这里每帧计算连招树的全局共享碰撞间隔  
            blackboard.CalculateGlobalSharedCollideInterval(deltaTime);
            
            // 优先判断并执行入口节点
            if (_currentNode == entry)
            {
                var entryTransitions = transitions.Where(transition => transition.from == _currentNode)
                    .ToList();
                var transition = entryTransitions.Find(transition => transition.Transit());
                if (transition)
                {
                    transition.Passed = true;
                    SwitchToNode(transition.to, transition, parameters);
                }
                else
                {
                    SwitchToNode(exit, null, parameters);
                }
            }

            // 在极限情况下存在一切换到节点就满足节点的过渡条件，为了允许同帧多次切换，就采用循环方式反复判断节点过渡条件
            while (_currentNode != exit)
            {
                // 判断当前节点是否能够过渡到其他节点，如果是则过渡到其他节点     
                var mayTransitTransitions = transitions.Where(transition => transition.from == _currentNode)
                    .ToList();
                var targetTransition = mayTransitTransitions.Find(transition => transition.Transit());
                if (targetTransition)
                {
                    targetTransition.Passed = true;
                    SwitchToNode(targetTransition.to, targetTransition, parameters);
                }
                else
                {
                    // 如果没有满足过渡条件，就判断当前节点是否是上一帧的节点，是则执行帧函数，如果不是的话说明当前节点在该帧恰好切换到，就不执行帧函数
                    if (previousNode != _currentNode)
                    {
                        // 直接返回播放状态
                        return ComboGraphState.Play;
                    }

                    _currentNode.Tick(deltaTime, parameters);
                    // 在执行节点帧逻辑后，再次判断是否满足过渡条件，满足就切换节点，否则就返回播放状态
                    targetTransition = mayTransitTransitions.Find(transition => transition.Transit());
                    if (targetTransition)
                    {
                        targetTransition.Passed = true;
                        SwitchToNode(targetTransition.to, targetTransition, parameters);
                    }
                    else
                    {
                        // 直接返回播放状态
                        return ComboGraphState.Play;
                    }
                }
            }

            // 到这里一定是退出节点，返回完成状态
            Playing = false;
            return ComboGraphState.Finish;
        }

        /// <summary>
        /// 连招图中断函数，用于中断连招
        /// </summary>
        /// <param name="parameters"></param>
        public void Abort(ComboGraphParameters parameters)
        {
            if (!Playing)
            {
                return;
            }

            // 中断当前节点
            _currentNode.Abort(parameters);
            // 直接跳到退出节点，无视过渡条件
            SwitchToNode(exit, null, parameters);
        }

        public List<ComboTip> GetEntryTips(bool matchSatisfyConditions)
        {
            if (entry)
            {
                var entryTips = new List<ComboTip>();
                // 获取入口节点的过渡条件
                var entryTransitions = transitions
                    .Where(transition => transition.from == entry && transition.to is ComboGraphPlayNode)
                    .ToList();
                // 获取对应的过渡条件
                var matchConditionsTransitions = matchSatisfyConditions
                    ? entryTransitions.Where(transition => transition.Transit(true)).ToList()
                    : entryTransitions;
                matchConditionsTransitions.ForEach(transition =>
                {
                    if (TryGetComboTipFromTransition(blackboard, transition, out var comboTip))
                    {
                        entryTips.Add(comboTip);
                    }
                });

                return entryTips;
            }

            return new List<ComboTip>();
        }

        public List<ComboTip> GetNextComboTips(ComboGraphPlayNode node)
        {
            if (node)
            {
                var entryTips = new List<ComboTip>();
                // 获取连招节点的过渡条件
                var nodeTransitions = transitions
                    .Where(transition => transition.from == node)
                    .Where(transition =>
                    {
                        // 这里额外过滤连招名称与当前连招名称相同的连招提示
                        if (transition.to is ComboGraphPlayNode playNode && playNode.comboConfig)
                        {
                            return playNode.comboConfig.Name != node.comboConfig.Name;
                        }

                        return false;
                    })
                    .ToList();
                nodeTransitions.ForEach(transition =>
                {
                    if (TryGetComboTipFromTransition(blackboard, transition, out var comboTip))
                    {
                        entryTips.Add(comboTip);
                    }
                });

                return entryTips;
            }

            return new List<ComboTip>();
        }

        private void SwitchToNode(ComboGraphNode node, ComboGraphTransition transition, ComboGraphParameters parameters)
        {
            DebugUtil.LogYellow($"From {_currentNode} to {node}, transition: {transition}");

            if (_currentNode)
            {
                _currentNode.Exit(node, transition, parameters);
            }

            if (node)
            {
                node.Enter(_currentNode, transition, parameters);
            }

            _currentNode = node;
        }

        private bool TryGetComboTipFromTransition(ComboBlackboard comboBlackboard, ComboGraphTransition transition,
            out ComboTip tip)
        {
            tip = default;
            // 如果过渡条件不是指向连招播放节点，就不返回数据
            if (transition.to is not ComboGraphPlayNode playNode)
            {
                return false;
            }

            // 匹配对应的连招条件
            var tips = new List<ComboInputTip>();
            transition.conditions.ForEach(condition =>
            {
                var tipIndex = Array.FindIndex(comboBlackboard.Tips, inputTip =>
                {
                    if (inputTip.key == condition.key &&
                        inputTip.type == condition.type)
                    {
                        switch (inputTip.type)
                        {
                            case BlackboardVariableType.Int:
                                return inputTip.intValue == condition.intCondition;
                            case BlackboardVariableType.Float:
                                return Mathf.Approximately(inputTip.floatValue,
                                    condition.floatCondition);
                            case BlackboardVariableType.Bool:
                                return inputTip.boolValue == condition.boolCondition;
                            case BlackboardVariableType.String:
                                return inputTip.stringValue == condition.stringCondition;
                        }
                    }

                    return false;
                });
                if (tipIndex != -1)
                {
                    tips.AddRange(comboBlackboard.Tips[tipIndex].tips);
                }
            });

            if (tips.Count == 0) return false;
            tip = new ComboTip
            {
                ComboConfig = playNode.comboConfig,
                OperatorTips = new List<ComboConditionOperatorTip>
                {
                    new()
                    {
                        OperatorType = BlackboardConditionOperatorType.And,
                        Tips = tips,
                    }
                },
            };
            return true;
        }
    }
}