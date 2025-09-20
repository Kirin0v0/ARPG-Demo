using System;
using System.Collections.Generic;
using System.Linq;
using Character;
using Character.Data;
using Framework.Common.Timeline;
using Framework.Common.Timeline.Clip;
using Framework.Common.Timeline.Data;
using Framework.Common.Timeline.Node;
using Framework.Common.Util;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using Skill.Condition;
using Skill.Runtime;
using Skill.Unit;
using UnityEditor;
using UnityEngine;
using VContainer;

namespace Skill
{
    [CreateAssetMenu(fileName = "Skill Flow", menuName = "Skill/Skill Flow")]
    public class SkillFlow : ScriptableObject
    {
        [ReadOnly] public SkillFlowRootNode rootNode;
        [ReadOnly] public List<SkillFlowNode> nodes = new();

#if UNITY_EDITOR
        [HideInInspector, SerializeField] private int timelineSeed;
#endif

        public string Id => rootNode ? rootNode.id : "";
        public string Name => rootNode ? rootNode.name : "";
        public string Description => rootNode ? rootNode.description : "";
        public SkillCost Cost => rootNode?.cost ?? SkillCost.Empty;
        public bool NeedTarget => rootNode?.needTarget ?? false;
        public SkillTargetGroup TargetGroup => rootNode?.targetGroup ?? SkillTargetGroup.None;

        // 运行时数据
        [NonSerialized] private string _runningNumber;

        public string GetRunningId(string id, bool isRoot = false)
        {
            return isRoot ? $"{_runningNumber}_{id}" : $"{_runningNumber}_{Id}_{id}";
        }

        [Inject] private TimelineManager _timelineManager;

        public TimelineManager TimelineManager
        {
            get
            {
                // 如果没有注入成功，就尝试去场景查找绑定该组件的游戏物体
                if (!_timelineManager)
                {
                    _timelineManager = GameEnvironment.FindEnvironmentComponent<TimelineManager>();
                }

                return _timelineManager;
            }
        }

#if UNITY_EDITOR
        public SkillFlowRootNode CreateRootNode()
        {
            return CreateNode(typeof(SkillFlowRootNode)) as SkillFlowRootNode;
        }

        public SkillFlowNode CreateNode(Type type)
        {
            Undo.RecordObject(this, "Skill Flow(Create Node)");
            var node = ScriptableObject.CreateInstance(type) as SkillFlowNode;
            node.skillFlow = this;
            node.name = type.Name;
            node.guid = GUID.Generate().ToString();
            nodes.Add(node);

            // 记录时间轴种子数，用于生成时间轴Id
            if (node is not SkillFlowRootNode)
            {
                timelineSeed++;
                node.id = $"n{timelineSeed}";
            }

            if (!Application.isPlaying) // 文件附加到另一资源文件需要在非运行状态才能执行
            {
                AssetDatabase.AddObjectToAsset(node, this);
            }

            Undo.RegisterCreatedObjectUndo(node, "Skill Flow(Create Node)");
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();

            return node;
        }

        public void DeleteNode(SkillFlowNode node)
        {
            Undo.RecordObject(this, "Skill Flow(Delete Node)");
            nodes.Remove(node);
            if (rootNode == node)
            {
                rootNode = null;
            }

            // AssetDatabase.RemoveObjectFromAsset(node);
            Undo.DestroyObjectImmediate(node);
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
        }

        public bool AddChildNode(SkillFlowNode parent, string key, SkillFlowNode child)
        {
            Undo.RecordObject(parent, "Skill Flow(Add Child Node)");
            var result = parent.AddChildNode(key, child);
            EditorUtility.SetDirty(parent);
            return result;
        }

        public bool RemoveChildNode(SkillFlowNode parent, string key, SkillFlowNode child)
        {
            Undo.RecordObject(parent, "Skill Flow(Remove Child Node)");
            var result = parent.RemoveChildNode(key, child);
            EditorUtility.SetDirty(parent);
            return result;
        }
#endif

        public bool MatchSkillPreconditions(CharacterObject caster,
            out BaseSkillPreconditionFailureReason failureReason)
        {
            failureReason = SkillPreconditionFailureReasons.Unknown;
            return rootNode && rootNode.MatchSkillPreconditions(caster, out failureReason);
        }

        public bool MatchSkillTarget(CharacterObject caster, [CanBeNull] CharacterObject target)
        {
            return rootNode && rootNode.MatchSkillTarget(caster, target);
        }

        public bool MatchSkillPostconditions(CharacterObject caster, [CanBeNull]CharacterObject target,
            out BaseSkillPostconditionFailureReason failureReason)
        {
            failureReason = SkillPostconditionFailureReasons.Unknown;
            return rootNode && rootNode.MatchSkillPostconditions(caster, target, out failureReason);
        }

        /// <summary>
        /// 技能条件流程：施法者预条件检测->目标检测(如果技能需要目标->目标后条件检测)
        /// </summary>
        /// <param name="caster"></param>
        /// <param name="target"></param>
        /// <param name="failureReason"></param>
        /// <returns></returns>
        public bool PassSkillConditionProcess(CharacterObject caster, [CanBeNull] CharacterObject target,
            out string failureReason)
        {
            failureReason = "";

            // 判断施法者是否满足预条件
            if (!MatchSkillPreconditions(caster, out var preconditionFailureReason))
            {
                failureReason = preconditionFailureReason.Message;
                return false;
            }

            // 判断目标是否符合条件
            if (!MatchSkillTarget(caster, target))
            {
                failureReason = "目标不符合技能目标条件";
                return false;
            }

            // 如果需要目标，则判断目标是否符合后条件
            if (NeedTarget)
            {
                if (!MatchSkillPostconditions(caster, target, out var postconditionFailureReason))
                {
                    failureReason = postconditionFailureReason.Message;
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 执行技能释放条件流程，通过流程后释放技能并返回技能释放对象，注意，其内部会采用Clone函数克隆全部子节点，避免多个角色同时释放该技能导致的ScriptableObject复用问题
        /// </summary>
        /// <param name="objectResolver">DI容器</param>
        /// <param name="group">技能分组</param>
        /// <param name="caster">施法角色</param>
        /// <param name="target">目标角色，允许为空</param>
        /// <returns>技能释放对象</returns>
        public bool TryReleaseSkill(
            IObjectResolver objectResolver,
            CharacterObject caster,
            [CanBeNull] CharacterObject target,
            out SkillReleaseInfo skillReleaseInfo
        )
        {
            skillReleaseInfo = null;
            // 克隆技能流并遍历子节点赋予运行时对象
            var runtimeSkillFlow = Clone();
            objectResolver.Inject(runtimeSkillFlow);
            runtimeSkillFlow._runningNumber = MathUtil.RandomId();
            runtimeSkillFlow.nodes.ForEach(node => objectResolver.Inject(node));
            runtimeSkillFlow.nodes.ForEach(node => node.Caster = caster);
            runtimeSkillFlow.nodes.ForEach(node => node.Target = target);

            // 判断是否通过技能条件流程
            if (!runtimeSkillFlow.PassSkillConditionProcess(caster, target, out var _))
            {
                return false;
            }

            // 释放技能
            var totalTimeline = runtimeSkillFlow.rootNode.ReleaseSkill();
            skillReleaseInfo = new SkillReleaseInfo(
                runtimeSkillFlow,
                totalTimeline,
                caster,
                target
            );
            return true;
        }

        public void StopSkill()
        {
            // 停止根节点的时间轴
            TimelineManager.StopTimeline(rootNode.RunningId);
            // 停止所有时间轴节点的时间轴
            nodes.OfType<SkillFlowTimelineNode>()
                .ForEach(node => TimelineManager.StopTimeline(node.RunningId));
        }

        public void SetSkillSpeed(float speed)
        {
            // 设置根节点的时间轴时间缩放
            TimelineManager.SetTimelineTimeScale(rootNode.RunningId, speed);
            // 设置所有时间轴节点的时间轴时间缩放
            nodes.OfType<SkillFlowTimelineNode>()
                .ForEach(node =>
                {
                    node.TimeScale = speed;
                    TimelineManager.SetTimelineTimeScale(node.RunningId, speed);
                });
        }

        public SkillFlowNode GetNode(string nodeId)
        {
            return nodes.FirstOrDefault(node => node.id == nodeId);
        }

        private SkillFlow Clone()
        {
            var skillFlow = Instantiate(this);
            // 节点克隆从根节点开始
            skillFlow.rootNode = rootNode.Clone() as SkillFlowRootNode;
            // 调用DFS遍历流程的全部节点
            var clonedNodes = new List<SkillFlowNode>();
            skillFlow.rootNode!.Visit(node =>
            {
                node.skillFlow = skillFlow;
                clonedNodes.Add(node);
            });
            skillFlow.nodes = clonedNodes;
            return skillFlow;
        }
    }
}