using System;
using System.Collections.Generic;
using AoE;
using AoE.Data;
using Character;
using Common;
using Framework.Common.BehaviourTree.Node;
using Framework.Common.Debug;
using Framework.Common.Timeline.Data;
using Sirenix.OdinInspector;
using UnityEngine;
using VContainer;

namespace Skill.Unit.Feature
{
    [NodeMenuItem("Feature/AoE")]
    public class SkillFlowFeatureAoENode : SkillFlowFeatureNode
    {
        public enum TargetStrategy
        {
            Caster,
            Target,
            Bullet,
        }

        public enum TargetCharacterPosition
        {
            Center,
            Top,
            Bottom,
        }

        private const string OnCreatePort = "onCreate";
        private const string OnCharactersEnterPort = "onCharactersEnter";
        private const string OnCharactersStayPort = "onCharactersStay";
        private const string OnCharactersLeavePort = "onCharactersLeave";
        private const string OnTickPort = "onTick";
        private const string OnDestroyPort = "onDestroy";

        [Title("AoE外观配置")] public GameObject prefab;
        public float prefabSimulationSpeed = 1f;
        public Vector3 prefabLocalPosition = Vector3.zero;
        public Vector3 prefabLocalEulerAngle = Vector3.zero;

        [Title("AoE生命周期配置")] [MinValue(0f)] public float tickTime;
        [MinValue(0f)] public float duration;
        [MinValue(0f)] public float destroyDelay;

        [Title("AoE目标配置")] public TargetStrategy targetStrategy;

        [ShowIf("@targetStrategy != TargetStrategy.Bullet", true, true)]
        public TargetCharacterPosition targetCharacterPosition = TargetCharacterPosition.Center;

        [ShowIf("@targetStrategy == TargetStrategy.Bullet", true, true)]
        public string bulletNodeId;

        public Vector3 targetRelativePosition;

        [Title("AoE命中配置")] public bool hitEnemy = true;
        public bool hitAlly = false;
        public bool hitSelf = false;

        [Title("AoE碰撞配置")] public AoEColliderType colliderType;

        // 盒状参数配置
        [ShowIf("colliderType", AoEColliderType.Box)] [BoxGroup("Box", false)]
        public Vector3 boxLocalRotation;

        [ShowIf("colliderType", AoEColliderType.Box)] [BoxGroup("Box", false)]
        public Vector3 boxSize;

        // 球状参数配置
        [ShowIf("colliderType", AoEColliderType.Sphere)] [BoxGroup("Sphere", false)]
        public float sphereRadius;

        // 扇柱参数配置
        [ShowIf("colliderType", AoEColliderType.Sector)] [BoxGroup("Sector", false)]
        public Vector3 sectorLocalRotation;

        [ShowIf("colliderType", AoEColliderType.Sector)] [BoxGroup("Sector", false)]
        public float sectorInsideRadius;

        [ShowIf("colliderType", AoEColliderType.Sector)] [BoxGroup("Sector", false)]
        public float sectorRadius;

        [ShowIf("colliderType", AoEColliderType.Sector)] [BoxGroup("Sector", false)]
        public float sectorHeight;

        [ShowIf("colliderType", AoEColliderType.Sector)] [BoxGroup("Sector", false)]
        public float sectorAngle;

        [Inject] private GameManager _gameManager;

        private GameManager GameManager
        {
            get
            {
                if (!_gameManager)
                {
                    _gameManager = GameEnvironment.FindEnvironmentComponent<GameManager>();
                }

                return _gameManager;
            }
        }

        public override string Title => "业务——AoE节点";

#if UNITY_EDITOR
        public override List<SkillFlowNodePort> GetOutputs()
        {
            return new List<SkillFlowNodePort>()
            {
                new SkillFlowNodePort
                {
                    key = OnCreatePort,
                    title = "创建时（支持时间轴/业务节点）",
                    capacity = SkillFlowNodePortCapacity.Multiple,
                },
                new SkillFlowNodePort
                {
                    key = OnCharactersEnterPort,
                    title = "角色进入时（支持业务节点）",
                    capacity = SkillFlowNodePortCapacity.Multiple,
                },
                new SkillFlowNodePort
                {
                    key = OnCharactersStayPort,
                    title = "角色停留时（支持业务节点）",
                    capacity = SkillFlowNodePortCapacity.Multiple,
                },
                new SkillFlowNodePort
                {
                    key = OnCharactersLeavePort,
                    title = "角色退出时（支持业务节点）",
                    capacity = SkillFlowNodePortCapacity.Multiple,
                },
                new SkillFlowNodePort
                {
                    key = OnTickPort,
                    title = "每帧执行时（支持业务节点）",
                    capacity = SkillFlowNodePortCapacity.Multiple,
                },
                new SkillFlowNodePort
                {
                    key = OnDestroyPort,
                    title = "销毁时（支持时间轴/业务节点）",
                    capacity = SkillFlowNodePortCapacity.Multiple,
                },
            };
        }

        public override bool AddChildNode(string key, SkillFlowNode child)
        {
            switch (key)
            {
                case OnCreatePort:
                case OnDestroyPort:
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
                case OnCharactersEnterPort:
                case OnCharactersStayPort:
                case OnCharactersLeavePort:
                case OnTickPort:
                {
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

            return false;
        }

        public override bool RemoveChildNode(string key, SkillFlowNode child)
        {
            switch (key)
            {
                case OnCreatePort:
                case OnDestroyPort:
                {
                    if (child is SkillFlowFeatureNode featureNode || child is SkillFlowTimelineNode timelineNode)
                    {
                        return RemoveChildNodeInternal(key, child);
                    }
                }
                    break;
                case OnCharactersEnterPort:
                case OnCharactersStayPort:
                case OnCharactersLeavePort:
                case OnTickPort:
                {
                    if (child is SkillFlowFeatureNode featureNode)
                    {
                        return RemoveChildNodeInternal(key, child);
                    }
                }
                    break;
            }

            return false;
        }
#endif

        public override ISkillFlowFeaturePayloadsRequire GetPayloadsRequire()
        {
            return SkillFlowFeaturePayloadsRequirements.EmptyPayloads;
        }

        protected override void OnExecute(TimelineInfo timelineInfo, object[] payloads)
        {
            var castPosition= Vector3.zero;
            switch (targetStrategy)
            {
                case TargetStrategy.Target:
                {
                    if (!Target)
                    {
                        DebugUtil.LogError("The target is not existing while target strategy is Target");
                        return;
                    }

                    castPosition = targetCharacterPosition switch
                    {
                        TargetCharacterPosition.Center => Target.Visual.TransformCenterPoint(targetRelativePosition),
                        TargetCharacterPosition.Top => Target.Visual.TransformTopPoint(targetRelativePosition),
                        TargetCharacterPosition.Bottom => Target.Visual.TransformBottomPoint(targetRelativePosition),
                        _ => Target.Visual.TransformBottomPoint(targetRelativePosition),
                    };
                }
                    break;
                case TargetStrategy.Bullet:
                {
                    var bulletNode = skillFlow.GetNode(bulletNodeId);
                    if (!bulletNode)
                    {
                        DebugUtil.LogError($"Can't find the Bullet node that matches the specified id({bulletNodeId})");
                        return;
                    }

                    var bulletObject = GameManager.GetBullet(bulletNode.RunningId);
                    if (!bulletObject)
                    {
                        DebugUtil.LogError(
                            $"Can't find the Bullet object that matches the specified id({bulletNode.RunningId})");
                        return;
                    }

                    castPosition = bulletObject.transform.position;
                }
                    break;
                default:
                case TargetStrategy.Caster:
                {
                    if (!Caster)
                    {
                        DebugUtil.LogError("The caster is not existing while target strategy is Caster");
                        return;
                    }

                    castPosition = targetCharacterPosition switch
                    {
                        TargetCharacterPosition.Center => Caster.Visual.TransformCenterPoint(targetRelativePosition),
                        TargetCharacterPosition.Top => Caster.Visual.TransformTopPoint(targetRelativePosition),
                        TargetCharacterPosition.Bottom => Caster.Visual.TransformBottomPoint(targetRelativePosition),
                        _ => Caster.Visual.TransformBottomPoint(targetRelativePosition),
                    };
                }
                    break;
            }

            // 创建AoE发射类并发射AoE
            var aoeInfo = new AoEInfo
            {
                id = RunningId,
                tickTime = tickTime,
                colliderType = colliderType,
                ColliderTypeParams = colliderType switch
                {
                    AoEColliderType.Box => new object[]
                    {
                        Vector3.zero,
                        boxLocalRotation,
                        boxSize
                    },
                    AoEColliderType.Sphere => new object[]
                    {
                        Vector3.zero,
                        sphereRadius
                    },
                    AoEColliderType.Sector => new object[]
                    {
                        Vector3.zero,
                        sectorLocalRotation,
                        sectorInsideRadius,
                        sectorRadius,
                        sectorHeight,
                        0f,
                        sectorAngle,
                    },
                },
                hitEnemy = hitEnemy,
                hitAlly = hitAlly,
                hitSelf = hitSelf,
                OnCreate = aoeObject =>
                {
                    ExecuteChildNodes(
                        OnCreatePort,
                        timelineInfo,
                        aoeObject.charactersInAoE
                    );
                },
                OnCharactersEnter = (aoeObject, characters) =>
                {
                    ExecuteChildNodes(
                        OnCharactersEnterPort,
                        timelineInfo,
                        characters
                    );
                },
                OnCharactersStay = (aoeObject, characters) =>
                {
                    ExecuteChildNodes(
                        OnCharactersStayPort,
                        timelineInfo,
                        characters
                    );
                },
                OnCharactersLeave = (aoeObject, characters) =>
                {
                    ExecuteChildNodes(
                        OnCharactersLeavePort,
                        timelineInfo,
                        characters
                    );
                },
                OnTick = aoeObject =>
                {
                    ExecuteChildNodes(
                        OnTickPort,
                        timelineInfo,
                        aoeObject.charactersInAoE
                    );
                },
                OnDestroy = aoeObject =>
                {
                    ExecuteChildNodes(
                        OnDestroyPort,
                        timelineInfo,
                        aoeObject.charactersInAoE
                    );
                },
            };
            var aoeLauncher = new AoELauncher(
                prefab,
                prefabSimulationSpeed,
                prefabLocalPosition,
                Quaternion.Euler(prefabLocalEulerAngle),
                Caster,
                castPosition,
                true,
                duration,
                destroyDelay,
                new Dictionary<string, object>
                {
                    { AoELauncher.Debug, debug }
                }
            );
            GameManager.CreateAoE(aoeLauncher, aoeInfo);
        }

        private void ExecuteChildNodes(
            string key,
            TimelineInfo timelineInfo,
            List<CharacterObject> characters
        )
        {
            GetChildNodes(key).ForEach(childNode =>
            {
                switch (childNode)
                {
                    case SkillFlowFeatureNode featureNode:
                    {
                        switch (featureNode.GetPayloadsRequire())
                        {
                            case SkillFlowFeatureNonPayloadsRequire nonPayloadsRequire:
                            {
                                featureNode.Execute(timelineInfo, nonPayloadsRequire.ProvideContext());
                            }
                                break;
                            case SkillFlowFeatureCharactersPayloadsRequire charactersPayloadsRequire:
                            {
                                featureNode.Execute(timelineInfo, charactersPayloadsRequire.ProvideContext(characters));
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
}