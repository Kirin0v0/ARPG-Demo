using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Action;
using Character;
using Framework.Common.Audio;
using Framework.Common.BehaviourTree.Node;
using Framework.Common.Debug;
using Framework.Common.Timeline.Data;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using Skill.Unit.Feature;
using UnityEngine;
using VContainer;

namespace Skill.Unit.TimelineClip
{
    [Serializable]
    public class SkillFlowActionColliderGroupSetting
    {
        [ReadOnly] public string groupId;
        public float detectionInterval;
        public int detectionMaximum;
    }

    [NodeMenuItem("Timeline Clip/Action")]
    public class SkillFlowTimelineClipActionNode : SkillFlowTimelineClipNode
    {
        private const string StartPort = "start";
        private const string AnticipationPort = "anticipation";
        private const string JudgmentPort = "judgment";
        private const string RecoveryPort = "recovery";
        private const string EndPort = "end";
        private const string ColliderTriggerPort = "colliderTrigger";

        public enum TargetType
        {
            Caster,
            Target,
        }

        [Title("动作配置")] public ActionClip actionClip;
        public TargetType targetType = TargetType.Caster;

        [Title("碰撞配置")] [InfoBox("统一设置动作片段中每个碰撞轨道内置的碰撞同一物体后的检测间隔（单位/秒）")]
        public float colliderDetectionInterval = 1f;

        [InfoBox("统一设置动作片段中每个碰撞轨道与碰撞体发生碰撞的最大数量，一般在指定数量（比如单体攻击）时调整数量")]
        public int colliderDetectionMaximum = 100;

        [ValueDropdown("GetUniqueColliderGroupConfigurableSettings")] [InfoBox("定制碰撞组数据，将不采用统一配置数据")]
        public List<SkillFlowActionColliderGroupSetting> colliderGroupSettings;

        public bool collideWithAlly = true;
        public bool collideWithEnemy = true;

        private TimelineInfo _timelineInfo;
        private ActionClipPlayer _actionClipPlayer;

        // 非全局的碰撞组检测记录，以碰撞组id为键
        private readonly Dictionary<string, List<(GameObject gameObject, float countdown)>> _detectedObjectRecords =
            new();

        [Title("事件配置"), ValueDropdown("GetUniqueEvents")]
        [InfoBox("这里每配置一个事件就会新增一个事件对应的输出端口，仅支持时间轴和无参业务节点的配置，配置后需要手动更新编辑器页面")]
        public List<string> events = new();

        public override string Title => "时间轴片段——动作节点";

#if UNITY_EDITOR
        public override List<SkillFlowNodePort> GetOutputs()
        {
            var ports = new List<SkillFlowNodePort>()
            {
                new SkillFlowNodePort
                {
                    key = StartPort,
                    title = "动作开始时（支持时间轴/业务节点）",
                    capacity = SkillFlowNodePortCapacity.Multiple,
                },
                new SkillFlowNodePort
                {
                    key = AnticipationPort,
                    title = "动作前摇时（支持时间轴/业务节点）",
                    capacity = SkillFlowNodePortCapacity.Multiple,
                },
                new SkillFlowNodePort
                {
                    key = JudgmentPort,
                    title = "动作判定时（支持时间轴/业务节点）",
                    capacity = SkillFlowNodePortCapacity.Multiple,
                },
                new SkillFlowNodePort
                {
                    key = RecoveryPort,
                    title = "动作后摇时（支持时间轴/业务节点）",
                    capacity = SkillFlowNodePortCapacity.Multiple,
                },
                new SkillFlowNodePort
                {
                    key = EndPort,
                    title = "动作结束时（支持时间轴/业务节点）",
                    capacity = SkillFlowNodePortCapacity.Multiple,
                },
                new SkillFlowNodePort
                {
                    key = ColliderTriggerPort,
                    title = "动作触发碰撞时（支持时间轴/业务节点）",
                    capacity = SkillFlowNodePortCapacity.Multiple,
                },
            };
            events.ForEach(eventName =>
            {
                ports.Add(new SkillFlowNodePort
                {
                    key = eventName,
                    title = $"事件{eventName}触发时（支持时间轴/业务节点）",
                    capacity = SkillFlowNodePortCapacity.Multiple,
                });
            });
            return ports;
        }

        public override bool AddChildNode(string key, SkillFlowNode child)
        {
            switch (key)
            {
                case StartPort:
                case AnticipationPort:
                case JudgmentPort:
                case RecoveryPort:
                case EndPort:
                {
                    if (child is SkillFlowTimelineNode timelineNode)
                    {
                        AddChildNodeInternal(key, child);
                        return true;
                    }

                    if (child is SkillFlowFeatureNode featureNode &&
                        featureNode.GetPayloadsRequire() is SkillFlowFeatureNonPayloadsRequire)
                    {
                        AddChildNodeInternal(key, child);
                        return true;
                    }
                }
                    break;
                case ColliderTriggerPort:
                {
                    if (child is SkillFlowTimelineNode timelineNode)
                    {
                        AddChildNodeInternal(key, child);
                        return true;
                    }

                    if (child is SkillFlowFeatureNode featureNode &&
                        (featureNode.GetPayloadsRequire() is SkillFlowFeatureNonPayloadsRequire ||
                         featureNode.GetPayloadsRequire() is SkillFlowFeatureCharactersPayloadsRequire))
                    {
                        AddChildNodeInternal(key, child);
                        return true;
                    }
                }
                    break;
            }

            foreach (var eventName in events)
            {
                if (key == eventName)
                {
                    if (child is SkillFlowTimelineNode timelineNode)
                    {
                        AddChildNodeInternal(key, child);
                        return true;
                    }

                    if (child is SkillFlowFeatureNode featureNode &&
                        featureNode.GetPayloadsRequire() is SkillFlowFeatureNonPayloadsRequire)
                    {
                        AddChildNodeInternal(key, child);
                        return true;
                    }
                }
            }

            return false;
        }

        public override bool RemoveChildNode(string key, SkillFlowNode child)
        {
            switch (key)
            {
                case StartPort:
                case AnticipationPort:
                case JudgmentPort:
                case RecoveryPort:
                case EndPort:
                {
                    if (child is SkillFlowFeatureNode featureNode || child is SkillFlowTimelineNode timelineNode)
                    {
                        return RemoveChildNodeInternal(key, child);
                    }
                }
                    break;
                case ColliderTriggerPort:
                {
                    if (child is SkillFlowFeatureNode featureNode || child is SkillFlowTimelineNode timelineNode)
                    {
                        return RemoveChildNodeInternal(key, child);
                    }
                }
                    break;
            }


            foreach (var eventName in events)
            {
                if (key == eventName)
                {
                    if (child is SkillFlowFeatureNode featureNode || child is SkillFlowTimelineNode timelineNode)
                    {
                        return RemoveChildNodeInternal(key, child);
                    }
                }
            }

            return false;
        }
#endif

        protected override void OnBegin(Framework.Common.Timeline.Clip.TimelineClip timelineClip,
            TimelineInfo timelineInfo)
        {
            _timelineInfo = timelineInfo;
            switch (targetType)
            {
                case TargetType.Caster:
                {
                    if (!Caster)
                    {
                        DebugUtil.LogError("The caster is not existing while targetType is Caster");
                        return;
                    }

                    _actionClipPlayer = new ActionClipPlayer(
                        actionClip,
                        Caster,
                        GlobalRuleSingletonConfigSO.Instance.characterHitLayer,
                        HandleActionClipCollide
                    );
                }
                    break;
                case TargetType.Target:
                {
                    if (!Target)
                    {
                        DebugUtil.LogError("The target is not existing while targetType is Target");
                        return;
                    }

                    _actionClipPlayer = new ActionClipPlayer(
                        actionClip,
                        Target,
                        GlobalRuleSingletonConfigSO.Instance.characterHitLayer,
                        HandleActionClipCollide
                    );
                }
                    break;
            }

            if (_actionClipPlayer != null)
            {
                _actionClipPlayer.OnStartStage += HandleActionClipStart;
                _actionClipPlayer.OnAnticipationStage += HandleActionClipAnticipation;
                _actionClipPlayer.OnJudgmentStage += HandleActionClipJudgment;
                _actionClipPlayer.OnRecoveryStage += HandleActionClipRecovery;
                _actionClipPlayer.OnEndStage += HandleActionClipEnd;
                _actionClipPlayer.OnStopStage += HandleActionClipEnd;
                _actionClipPlayer.RegisterEventListener(HandleActionEventTrigger);
                _actionClipPlayer.Start();
            }
        }

        protected override void OnTick(Framework.Common.Timeline.Clip.TimelineClip timelineClip,
            TimelineInfo timelineInfo)
        {
            _actionClipPlayer?.Tick(timelineClip.TickTime);
        }

        protected override void OnEnd(Framework.Common.Timeline.Clip.TimelineClip timelineClip,
            TimelineInfo timelineInfo)
        {
            if (_actionClipPlayer != null)
            {
                _actionClipPlayer.Stop();
                _actionClipPlayer.OnStartStage -= HandleActionClipStart;
                _actionClipPlayer.OnAnticipationStage -= HandleActionClipAnticipation;
                _actionClipPlayer.OnJudgmentStage -= HandleActionClipJudgment;
                _actionClipPlayer.OnRecoveryStage -= HandleActionClipRecovery;
                _actionClipPlayer.OnEndStage -= HandleActionClipEnd;
                _actionClipPlayer.OnStopStage -= HandleActionClipEnd;
                _actionClipPlayer.UnregisterEventListener(HandleActionEventTrigger);
            }

            _timelineInfo = null;
            _actionClipPlayer = null;
        }

        private void HandleActionClipCollide(string groupId, Collider collider)
        {
            // 只有与角色碰撞才会执行后续逻辑
            if (!collider.TryGetHitCharacter(out var targetCharacter, out var damageMultiplier, out var priority))
            {
                return;
            }

            // 屏蔽自身碰撞
            var self = targetType switch
            {
                TargetType.Caster => Caster,
                TargetType.Target => Target,
                _ => Caster
            };
            if (targetCharacter == self)
            {
                return;
            }
            
            // 过滤屏蔽阵营的碰撞
            if ((targetCharacter.Parameters.side == self!.Parameters.side && !collideWithAlly) || 
                (targetCharacter.Parameters.side != self!.Parameters.side && !collideWithEnemy))
            {
                return;
            }

            // 获取碰撞组的数据
            var index = colliderGroupSettings.FindIndex(setting => setting.groupId == groupId);
            if (index != -1)
            {
                colliderDetectionInterval = colliderGroupSettings[index].detectionInterval;
                colliderDetectionMaximum = colliderGroupSettings[index].detectionMaximum;
            }

            // 判断当前碰撞组是否存在碰撞记录
            if (_detectedObjectRecords.TryGetValue(groupId, out var detectedRecords))
            {
                // 判断已记录的碰撞体数量是否达到最大值，是则不响应碰撞
                if (detectedRecords.Count >= colliderDetectionMaximum)
                {
                    return;
                }

                // 判断该碰撞体是否已经被记录
                if (detectedRecords.Any((tuple => tuple.gameObject == collider.gameObject)))
                {
                    return;
                }

                detectedRecords.Add((collider.gameObject, colliderDetectionInterval));
                TriggerChildNodes();
                return;
            }

            _detectedObjectRecords.Add(groupId, new List<(GameObject gameObject, float countdown)>
            {
                new ValueTuple<GameObject, float>(collider.gameObject, colliderDetectionInterval)
            });
            TriggerChildNodes();

            return;

            void TriggerChildNodes()
            {
                GetChildNodes(ColliderTriggerPort).ForEach(childNode =>
                {
                    switch (childNode)
                    {
                        case SkillFlowFeatureNode featureNode:
                        {
                            switch (featureNode.GetPayloadsRequire())
                            {
                                case SkillFlowFeatureNonPayloadsRequire nonPayloadsRequire:
                                {
                                    featureNode.Execute(_timelineInfo, nonPayloadsRequire.ProvideContext());
                                }
                                    break;
                                case SkillFlowFeatureCharactersPayloadsRequire charactersPayloadsRequire:
                                {
                                    featureNode.Execute(_timelineInfo,
                                        charactersPayloadsRequire.ProvideContext(new List<CharacterObject>
                                            { targetCharacter }));
                                }
                                    break;
                            }
                        }
                            break;
                        case SkillFlowTimelineNode timelineNode:
                        {
                            timelineNode.StartTimeline(Caster.gameObject);
                        }
                            break;
                    }
                });
            }
        }

        private void HandleActionClipStart()
        {
            GetChildNodes(StartPort).ForEach(childNode =>
            {
                switch (childNode)
                {
                    case SkillFlowFeatureNode featureNode:
                    {
                        switch (featureNode.GetPayloadsRequire())
                        {
                            case SkillFlowFeatureNonPayloadsRequire nonPayloadsRequire:
                            {
                                featureNode.Execute(_timelineInfo, nonPayloadsRequire.ProvideContext());
                            }
                                break;
                        }
                    }
                        break;
                    case SkillFlowTimelineNode timelineNode:
                    {
                        timelineNode.StartTimeline(Caster.gameObject);
                    }
                        break;
                }
            });
        }

        private void HandleActionClipAnticipation()
        {
            GetChildNodes(AnticipationPort).ForEach(childNode =>
            {
                switch (childNode)
                {
                    case SkillFlowFeatureNode featureNode:
                    {
                        switch (featureNode.GetPayloadsRequire())
                        {
                            case SkillFlowFeatureNonPayloadsRequire nonPayloadsRequire:
                            {
                                featureNode.Execute(_timelineInfo, nonPayloadsRequire.ProvideContext());
                            }
                                break;
                        }
                    }
                        break;
                    case SkillFlowTimelineNode timelineNode:
                    {
                        timelineNode.StartTimeline(Caster.gameObject);
                    }
                        break;
                }
            });
        }

        private void HandleActionClipJudgment()
        {
            GetChildNodes(JudgmentPort).ForEach(childNode =>
            {
                switch (childNode)
                {
                    case SkillFlowFeatureNode featureNode:
                    {
                        switch (featureNode.GetPayloadsRequire())
                        {
                            case SkillFlowFeatureNonPayloadsRequire nonPayloadsRequire:
                            {
                                featureNode.Execute(_timelineInfo, nonPayloadsRequire.ProvideContext());
                            }
                                break;
                        }
                    }
                        break;
                    case SkillFlowTimelineNode timelineNode:
                    {
                        timelineNode.StartTimeline(Caster.gameObject);
                    }
                        break;
                }
            });
        }

        private void HandleActionClipRecovery()
        {
            GetChildNodes(RecoveryPort).ForEach(childNode =>
            {
                switch (childNode)
                {
                    case SkillFlowFeatureNode featureNode:
                    {
                        switch (featureNode.GetPayloadsRequire())
                        {
                            case SkillFlowFeatureNonPayloadsRequire nonPayloadsRequire:
                            {
                                featureNode.Execute(_timelineInfo, nonPayloadsRequire.ProvideContext());
                            }
                                break;
                        }
                    }
                        break;
                    case SkillFlowTimelineNode timelineNode:
                    {
                        timelineNode.StartTimeline(Caster.gameObject);
                    }
                        break;
                }
            });
        }

        private void HandleActionClipEnd()
        {
            GetChildNodes(EndPort).ForEach(childNode =>
            {
                switch (childNode)
                {
                    case SkillFlowFeatureNode featureNode:
                    {
                        switch (featureNode.GetPayloadsRequire())
                        {
                            case SkillFlowFeatureNonPayloadsRequire nonPayloadsRequire:
                            {
                                featureNode.Execute(_timelineInfo, nonPayloadsRequire.ProvideContext());
                            }
                                break;
                        }
                    }
                        break;
                    case SkillFlowTimelineNode timelineNode:
                    {
                        timelineNode.StartTimeline(Caster.gameObject);
                    }
                        break;
                }
            });
        }

        private void HandleActionEventTrigger(string eventName, ActionEventParameter parameter, object payload)
        {
            GetChildNodes(eventName).ForEach(childNode =>
            {
                switch (childNode)
                {
                    case SkillFlowFeatureNode featureNode:
                    {
                        switch (featureNode.GetPayloadsRequire())
                        {
                            case SkillFlowFeatureNonPayloadsRequire nonPayloadsRequire:
                            {
                                featureNode.Execute(_timelineInfo, nonPayloadsRequire.ProvideContext());
                            }
                                break;
                        }
                    }
                        break;
                    case SkillFlowTimelineNode timelineNode:
                    {
                        timelineNode.StartTimeline(Caster.gameObject);
                    }
                        break;
                }
            });
        }

        private IEnumerable GetUniqueColliderGroupConfigurableSettings()
        {
            var items = new List<ValueDropdownItem>();
            if (!actionClip)
            {
                return items;
            }

            actionClip.collideDetection.collideDetectionClips.GroupBy(clip => clip.groupId).ForEach(group =>
            {
                if (colliderGroupSettings.FindIndex(setting => String.Equals(setting.groupId, group.Key)) != -1)
                {
                    return;
                }

                items.Add(new ValueDropdownItem
                {
                    Text = group.Key,
                    Value = new SkillFlowActionColliderGroupSetting
                    {
                        groupId = group.Key,
                        detectionInterval = colliderDetectionInterval,
                        detectionMaximum = colliderDetectionMaximum,
                    },
                });
            });

            return items;
        }

        private IEnumerable GetUniqueEvents()
        {
            var items = new List<ValueDropdownItem>();
            if (!actionClip)
            {
                return items;
            }

            actionClip.events.eventClips.ForEach(eventClip =>
            {
                if (events.Contains(eventClip.name))
                {
                    return;
                }

                items.Add(new ValueDropdownItem
                {
                    Text = eventClip.name,
                    Value = eventClip.name,
                });
            });
            return items;
        }

        private void OnValidate()
        {
            // 如果配置了动作片段，则默认采用其内部配置好的时长
            if (actionClip)
            {
                totalTicks = Mathf.RoundToInt(actionClip.duration / TickTime);
            }
        }
    }
}