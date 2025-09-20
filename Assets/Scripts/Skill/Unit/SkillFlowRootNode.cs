using System;
using System.Collections.Generic;
using System.Linq;
using Character;
using Character.Data;
using Framework.Common.BehaviourTree.Node;
using Framework.Common.Timeline;
using Framework.Common.Timeline.Data;
using Framework.Common.Timeline.Node;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using Skill.Condition;
using UnityEngine;

namespace Skill.Unit
{
    [Serializable]
    public struct SkillCost
    {
        public int hp; // hp
        public int mp; // mp
        public float atb; // Atb量

        public static SkillCost Empty = new SkillCost
        {
            hp = 0,
            mp = 0,
            atb = 0,
        };

        public CharacterResource Cost()
        {
            return new CharacterResource
            {
                hp = -hp,
                mp = -mp,
                atb = -atb,
            };
        }

        public CharacterResource ToResource()
        {
            return new CharacterResource
            {
                hp = hp,
                mp = mp,
                atb = atb,
            };
        }
    }

    public enum SkillScene
    {
        Land,
        Airborne,
        All,
    }

    [Flags]
    public enum SkillTargetGroup
    {
        None = 0,
        Self = 1 << 0,
        Ally = 1 << 1,
        Enemy = 1 << 2,
    }

    [Serializable]
    public class SkillFlowTimelineTransition
    {
        [ReadOnly] public string previous;
        [ReadOnly] public string next;
        public float interval;
    }

    [NodeMenuItem("Skill")]
    public class SkillFlowRootNode : SkillFlowNode
    {
        private const string TimelinePort = "timeline";

        public override string RunningId => skillFlow.GetRunningId(id, true);

        [Title("技能配置")] public new string name;

        [TextArea] public string description;

        [BoxGroup("技能预条件配置")] [InfoBox("技能消耗量，正数即是对施法者的资源消耗，负数则是资源补偿")]
        public SkillCost cost = SkillCost.Empty;

        [BoxGroup("技能预条件配置")] [InfoBox("是否存在额外技能资源条件，默认不存在，仅采用消耗量来判断资源条件")]
        public bool withExtraResourceCondition = false;

        [BoxGroup("技能预条件配置")] [ShowIf("withExtraResourceCondition", true, true)]
        public CharacterResource extraResourceCondition = CharacterResource.Empty;

        [BoxGroup("技能预条件配置")] [InfoBox("技能释放场景，仅在对应场景才能释放技能")]
        public SkillScene sceneCondition = SkillScene.All;

        [BoxGroup("技能目标配置")] [InfoBox("是否需要目标，如果需要则强制要求技能释放时传入目标角色，否则无法释放技能")]
        public bool needTarget = false;

        [BoxGroup("技能目标配置")] [ShowIf("needTarget", true, true)]
        public SkillTargetGroup targetGroup = SkillTargetGroup.None;

        [ShowIf("needTarget")] [BoxGroup("技能后条件配置")] [TypeFilter("GetPostconditionFilteredTypeList")]
        public List<BaseSkillPostcondition> postconditions = new();

        [Title("子时间轴过渡配置")] public List<SkillFlowTimelineTransition> timelineTransitions;

        public override string Title => "技能节点";

#if UNITY_EDITOR
        public override List<SkillFlowNodePort> GetOutputs()
        {
            return new List<SkillFlowNodePort>()
            {
                new SkillFlowNodePort
                {
                    key = TimelinePort,
                    title = "时间轴列表",
                    capacity = SkillFlowNodePortCapacity.Multiple,
                }
            };
        }

        public override bool AddChildNode(string key, SkillFlowNode child)
        {
            if (key == TimelinePort && child is SkillFlowTimelineNode timelineNode)
            {
                AddChildNodeInternal(key, timelineNode);
                timelineNode.executeOrder = GetChildNodes(key).Count;
                return true;
            }

            return false;
        }

        public override bool RemoveChildNode(string key, SkillFlowNode child)
        {
            if (key == TimelinePort && child is SkillFlowTimelineNode timelineNode)
            {
                var remove = RemoveChildNodeInternal(key, timelineNode);
                timelineNode.executeOrder = 1;
                // 剩余子节点执行顺序重新设置
                var children = GetChildNodes(key);
                for (var i = 0; i < children.Count; i++)
                {
                    var node = children[i] as SkillFlowTimelineNode;
                    node!.executeOrder = i + 1;
                }

                return remove;
            }

            return false;
        }

        public void SortChildTimelines()
        {
            // 排序子节点
            var children = GetChildNodes(TimelinePort);
            children.Sort((topNode, bottomNode) =>
                topNode.position.y <= bottomNode.position.y ? -1 : 1);

            // 子节点执行顺序重新设置
            for (var i = 0; i < children.Count; i++)
            {
                var childNode = children[i] as SkillFlowTimelineNode;
                childNode!.executeOrder = i + 1;
            }

            // 重新设置时间轴过渡
            var oldTransitions = timelineTransitions;
            timelineTransitions = new List<SkillFlowTimelineTransition>();
            for (int previousIndex = 0, nextIndex = 1;
                 previousIndex < children.Count - 1 && nextIndex < children.Count;
                 previousIndex++, nextIndex++)
            {
                var transition = new SkillFlowTimelineTransition
                {
                    previous = (children[previousIndex] as SkillFlowTimelineNode)!.id,
                    next = (children[nextIndex] as SkillFlowTimelineNode)!.id,
                    interval = 0
                };

                // 查询先前是否已经配置，是则将间隔改为配置值
                var oldTransition = oldTransitions.Find(transition =>
                    transition.previous == (children[previousIndex] as SkillFlowTimelineNode)!.id &&
                    transition.next == (children[nextIndex] as SkillFlowTimelineNode)!.id
                );
                if (oldTransition != null)
                {
                    transition.interval = oldTransition.interval;
                }

                timelineTransitions.Add(transition);
            }
        }
#endif

        public bool MatchSkillPreconditions(CharacterObject caster,
            out BaseSkillPreconditionFailureReason failureReason)
        {
            failureReason = SkillPreconditionFailureReasons.Unknown;
            if (!Application.isPlaying)
            {
                return true;
            }

            if (caster.ResourceAbility == null || !caster.ResourceAbility.Enough(cost.ToResource()))
            {
                failureReason = SkillPreconditionFailureReasons.ResourceNotEnough;
                return false;
            }

            if (withExtraResourceCondition && !caster.ResourceAbility.Enough(extraResourceCondition))
            {
                failureReason = SkillPreconditionFailureReasons.ResourceNotEnough;
                return false;
            }

            if (sceneCondition == SkillScene.Airborne && !caster.Parameters.Airborne)
            {
                failureReason = SkillPreconditionFailureReasons.SceneNotMatchAirborne;
                return false;
            }

            if (sceneCondition == SkillScene.Land && caster.Parameters.Airborne)
            {
                failureReason = SkillPreconditionFailureReasons.SceneNotMatchLand;
                return false;
            }

            return true;
        }

        public bool MatchSkillTarget(CharacterObject caster, [CanBeNull] CharacterObject target)
        {
            if (!needTarget)
            {
                return true;
            }

            if (target == null)
            {
                return false;
            }

            if (caster == target && (targetGroup & SkillTargetGroup.Self) != 0)
            {
                return true;
            }

            if (caster != target && caster.Parameters.side == target.Parameters.side &&
                (targetGroup & SkillTargetGroup.Ally) != 0)
            {
                return true;
            }

            if (caster.Parameters.side != target.Parameters.side &&
                (targetGroup & SkillTargetGroup.Enemy) != 0)
            {
                return true;
            }

            return false;
        }

        public bool MatchSkillPostconditions(CharacterObject caster, [CanBeNull]CharacterObject target,
            out BaseSkillPostconditionFailureReason failureReason)
        {
            failureReason = SkillPostconditionFailureReasons.Unknown;
            if (!needTarget || !Application.isPlaying)
            {
                return true;
            }

            if (!target)
            {
                failureReason = SkillPostconditionFailureReasons.NoTarget;
                return false;
            }

            foreach (var postcondition in postconditions)
            {
                if (!postcondition.MatchSkillPreconditions(skillFlow, caster, target, out failureReason))
                {
                    return false;
                }
            }

            return true;
        }

        public TimelineInfo ReleaseSkill()
        {
            // 调整释放角色资源
            Caster.ResourceAbility.Modify(cost.Cost());
            // 释放技能本质上就是在一个技能总时间轴内依次添加节点以动态添加时间轴
            var timelines = new List<TimelineNode>();
            var totalTimelineDuration = 0f;
            var children = GetChildNodes(TimelinePort);
            for (var i = 0; i < children.Count; i++)
            {
                var previousIndex = i - 1;
                var childNode = children[i] as SkillFlowTimelineNode;
                if (previousIndex >= 0)
                {
                    totalTimelineDuration += previousIndex < timelineTransitions.Count
                        ? timelineTransitions[previousIndex].interval
                        : 0f;
                }

                timelines.Add(new TimelineDelegateNode(totalTimelineDuration,
                    info => { childNode?.StartTimeline(Caster.gameObject); }));
                totalTimelineDuration += childNode.duration;
            }

            // 最终添加技能总时间轴
            return TimelineManager.StartTimeline(new Timeline
            {
                Id = RunningId,
                Clips = Array.Empty<Framework.Common.Timeline.Clip.TimelineClip>(),
                Nodes = timelines.ToArray(),
                Duration = totalTimelineDuration
            }, Caster.gameObject);
        }

        private IEnumerable<Type> GetPostconditionFilteredTypeList()
        {
            var q = typeof(BaseSkillPostcondition).Assembly.GetTypes()
                .Where(x => !x.IsAbstract)
                .Where(x => !x.IsGenericTypeDefinition)
                .Where(x => typeof(BaseSkillPostcondition).IsAssignableFrom(x));
            return q;
        }
    }
}